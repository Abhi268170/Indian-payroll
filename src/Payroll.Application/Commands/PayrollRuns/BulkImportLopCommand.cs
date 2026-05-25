using MediatR;
using Payroll.Application.DTOs;
using Payroll.Application.Services;
using Payroll.Application.Utilities;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Payroll.Engine;

namespace Payroll.Application.Commands.PayrollRuns;

public record BulkImportLopCommand(Guid RunId, string CsvContent, Guid ActorId) : IRequest<ImportResult>;

public sealed class BulkImportLopCommandHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IEmployeeRepository employeeRepo,
    IPayScheduleRepository payScheduleRepo,
    IPayrollRecomputeService recomputeService,
    IPayrollCostCalculator costCalculator,
    IUnitOfWork uow)
    : IRequestHandler<BulkImportLopCommand, ImportResult>
{
    public async Task<ImportResult> Handle(BulkImportLopCommand req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        if (run.Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException("Variable inputs can only be changed on a Draft payroll run.");

        var paySchedule = await payScheduleRepo.GetAsync(ct)
            ?? throw new DomainException("Pay Schedule not configured.");

        EngineSalaryCalculationMethod engineCalcMethod = paySchedule.SalaryCalculationMethod == SalaryCalculationMethod.ActualDays
            ? EngineSalaryCalculationMethod.ActualDays
            : EngineSalaryCalculationMethod.FixedDays;
        int salaryDivisor = PayScheduleHelpers.GetDivisor(engineCalcMethod, paySchedule.FixedWorkingDaysPerMonth, run.PayPeriod.Year, run.PayPeriod.Month);

        // Parse CSV
        IReadOnlyList<string[]> rows = CsvParser.Parse(req.CsvContent);

        // Batch-load all referenced employees
        List<string> employeeCodes = rows
            .Select(r => r.Length > 0 ? r[0] : string.Empty)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        IReadOnlyList<Employee> employees = await employeeRepo.GetManyByCodesAsync(employeeCodes, ct);
        Dictionary<string, Employee> empByCode = employees.ToDictionary(e => e.EmployeeCode, StringComparer.OrdinalIgnoreCase);

        // Batch-load all payrun employees
        IReadOnlyList<PayrunEmployee> allPayrunEmployees = await payrunEmployeeRepo.GetByRunIdAsync(req.RunId, ct);
        Dictionary<Guid, PayrunEmployee> payrunEmpById = allPayrunEmployees.ToDictionary(e => e.EmployeeId);

        int applied = 0;
        List<ImportRowError> errors = [];

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            string[] row = rows[rowIndex];
            int displayRow = rowIndex + 2; // 1-based + header offset

            string employeeCode = row.Length > 0 ? row[0] : string.Empty;
            string lopDaysRaw = row.Length > 1 ? row[1] : string.Empty;

            if (string.IsNullOrWhiteSpace(employeeCode))
            {
                errors.Add(new(displayRow, string.Empty, "Employee Code is required."));
                continue;
            }

            if (!int.TryParse(lopDaysRaw, out int lopDays) || lopDays < 0)
            {
                errors.Add(new(displayRow, employeeCode, "LOP Days must be a non-negative whole number."));
                continue;
            }

            if (!empByCode.TryGetValue(employeeCode, out Employee? employee))
            {
                errors.Add(new(displayRow, employeeCode, "No employee was found with this Employee Code."));
                continue;
            }

            if (!payrunEmpById.TryGetValue(employee.Id, out PayrunEmployee? payrunEmp))
            {
                errors.Add(new(displayRow, employeeCode, "This employee is not included in the selected payroll run."));
                continue;
            }

            if (payrunEmp.Status == PayrunEmployeeStatus.Skipped)
            {
                errors.Add(new(displayRow, employeeCode, "This employee is currently skipped in the payroll run."));
                continue;
            }

            if (lopDays >= salaryDivisor)
            {
                errors.Add(new(displayRow, employeeCode, $"LOP days ({lopDays}) must be less than the payable days for this month ({salaryDivisor})."));
                continue;
            }

            // Apply LOP, then re-run engine via shared service.
            payrunEmp.SetLop(lopDays, req.ActorId);

            RecomputeResult recompute = await recomputeService.RecomputeEmployeeAsync(req.RunId, employee.Id, ct);
            var result = recompute.Engine;

            payrunEmp.UpdateComputedAmounts(
                grossPay: result.Gross.GrossWage,
                taxableGrossPay: result.Gross.TaxableGrossWage,
                netPay: recompute.NetPayWithAdjustments,
                taxesAmount: result.TDS.MonthlyTDS + result.PT.Amount,
                benefitsAmount: result.PF.EPFEmployerContribution + result.ESI.EmployerContribution,
                reimbursementsAmount: recompute.ReimbursementsAmount,
                employeePf: result.PF.EmployeeContribution,
                employerPf: result.PF.EPFEmployerContribution,
                employeeEsi: result.ESI.EmployeeContribution,
                employerEsi: result.ESI.EmployerContribution,
                ptAmount: result.PT.Amount,
                tdsAmount: result.TDS.MonthlyTDS,
                lwfEmployeeAmount: result.LWF.EmployeeAmount,
                lwfEmployerAmount: result.LWF.EmployerAmount,
                gratuityAmount: result.Gratuity.MonthlyAccrual,
                epsAmount: result.PF.EPSEmployerContribution,
                monthlyCTC: payrunEmp.MonthlyCTC,
                actorId: req.ActorId);

            payrunEmployeeRepo.Update(payrunEmp);
            applied++;
        }

        // Update run summary once with final state of all active employees
        var activeEmployees = allPayrunEmployees.Where(e => e.Status == PayrunEmployeeStatus.Active).ToList();
        var snapshot = costCalculator.Calculate(activeEmployees);
        run.UpdateFinancialSummary(
            payrollCost: snapshot.PayrollCost,
            totalNetPay: snapshot.TotalNet,
            totalEmployerPf: snapshot.TotalEmployerPf,
            totalEmployerEsi: snapshot.TotalEmployerEsi,
            totalTds: snapshot.TotalTds,
            totalPt: snapshot.TotalPt,
            employeeCount: snapshot.EmployeeCount,
            actorId: req.ActorId);
        runRepo.Update(run);

        if (applied > 0)
            await uow.SaveChangesAsync(ct);

        return new ImportResult(applied, errors);
    }
}
