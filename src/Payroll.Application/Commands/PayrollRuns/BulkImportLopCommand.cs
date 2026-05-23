using MediatR;
using Payroll.Application.DTOs;
using Payroll.Application.Services;
using Payroll.Application.Utilities;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Payroll.Engine;
using Payroll.Engine.Inputs;
using System.Text.Json;

namespace Payroll.Application.Commands.PayrollRuns;

public record BulkImportLopCommand(Guid RunId, string CsvContent, Guid ActorId) : IRequest<ImportResult>;

public sealed class BulkImportLopCommandHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IPayrunComponentBreakdownRepository breakdownRepo,
    IEmployeeRepository employeeRepo,
    IWorkLocationRepository workLocationRepo,
    IPayScheduleRepository payScheduleRepo,
    IEmployeeFyOpeningRepository fyOpeningRepo,
    ITdsWorksheetRepository tdsWorksheetRepo,
    IUnitOfWork uow)
    : IRequestHandler<BulkImportLopCommand, ImportResult>
{
    public async Task<ImportResult> Handle(BulkImportLopCommand req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        if (run.Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException("Variable inputs can only be changed on a Draft payroll run.");

        if (run.StatutoryConfigSnapshot is null)
            throw new InvalidOperationException("Payroll run is missing statutory config snapshot.");

        StatutoryConfig staticConfig = JsonSerializer.Deserialize<StatutoryConfig>(run.StatutoryConfigSnapshot)!;

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

        // Batch-load work locations
        IReadOnlyList<WorkLocation> workLocations = await workLocationRepo.ListAsync(ct);
        Dictionary<Guid, string> stateByLocationId = workLocations.ToDictionary(
            wl => wl.Id,
            wl => wl.State.ToString());

        // Batch-load breakdowns for all employees in run
        Dictionary<Guid, IReadOnlyList<PayrunComponentBreakdown>> breakdownsByEmpId = [];
        foreach (PayrunEmployee pe in allPayrunEmployees)
        {
            breakdownsByEmpId[pe.EmployeeId] = await breakdownRepo.GetByRunAndEmployeeAsync(req.RunId, pe.EmployeeId, ct);
        }

        // YTD data
        Dictionary<Guid, (decimal YtdGross, decimal YtdTds)> ytdMap =
            await payrunEmployeeRepo.GetCurrentEmployerYtdAsync(employees.Select(e => e.Id), run.PayPeriod.FiscalYear, ct);

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
                errors.Add(new(displayRow, employeeCode, "Employee not found."));
                continue;
            }

            if (!payrunEmpById.TryGetValue(employee.Id, out PayrunEmployee? payrunEmp))
            {
                errors.Add(new(displayRow, employeeCode, "Employee not in this payroll run."));
                continue;
            }

            if (payrunEmp.Status == PayrunEmployeeStatus.Skipped)
            {
                errors.Add(new(displayRow, employeeCode, "Employee is skipped."));
                continue;
            }

            if (lopDays >= salaryDivisor)
            {
                errors.Add(new(displayRow, employeeCode, $"LOP days ({lopDays}) must be less than the salary divisor ({salaryDivisor})."));
                continue;
            }

            // Apply LOP
            payrunEmp.SetLop(lopDays, req.ActorId);

            string workStateCode = stateByLocationId.GetValueOrDefault(employee.WorkLocationId, "MH");

            // Incorporate FY opening into YTD
            ytdMap.TryGetValue(employee.Id, out (decimal YtdGross, decimal YtdTds) ytd);
            EmployeeFyOpening? fyOpening = await fyOpeningRepo.GetAsync(employee.Id, run.PayPeriod.FiscalYear, ct);
            if (fyOpening is not null)
            {
                ytd = (ytd.YtdGross + fyOpening.GrossSalary, ytd.YtdTds + fyOpening.TdsDeducted);
            }

            var breakdowns = breakdownsByEmpId[employee.Id];
            var result = SetLopCommandHandler.RecomputeEmployee(
                employee, workStateCode, payrunEmp, breakdowns, run, staticConfig, salaryDivisor,
                ytd.YtdGross, ytd.YtdTds);

            payrunEmp.UpdateComputedAmounts(
                grossPay: result.Gross.GrossWage,
                netPay: result.NetPay,
                taxesAmount: result.TDS.MonthlyTDS + result.PT.Amount,
                benefitsAmount: result.PF.EPFEmployerContribution + result.ESI.EmployerContribution,
                reimbursementsAmount: payrunEmp.ReimbursementsAmount,
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

            foreach (var bd in breakdowns.Where(b => !b.IsOneTimeEarning))
            {
                var computed = result.Gross.ComponentBreakdown.FirstOrDefault(c => c.ComponentId == bd.SalaryComponentId);
                if (computed is not null)
                    bd.UpdateAmounts(computed.FullAmount, computed.ProratedAmount);
            }

            payrunEmployeeRepo.Update(payrunEmp);

            // Upsert TDS worksheet
            await tdsWorksheetRepo.DeleteByRunAndEmployeeAsync(req.RunId, employee.Id, ct);
            await tdsWorksheetRepo.AddAsync(TdsWorksheet.Create(
                payrollRunId: req.RunId,
                employeeId: employee.Id,
                tenantId: payrunEmp.TenantId,
                fiscalYear: run.PayPeriod.FiscalYear,
                annualProjectedIncome: result.TDS.TaxableIncome + staticConfig.StandardDeduction,
                standardDeduction: staticConfig.StandardDeduction,
                taxableIncome: result.TDS.TaxableIncome,
                taxBeforeRebate: result.TDS.TaxBeforeRebate,
                rebate87A: result.TDS.Rebate87AApplied ? Math.Min(result.TDS.TaxBeforeRebate, staticConfig.Rebate87AAmount) : 0m,
                surcharge: result.TDS.Surcharge,
                cess: result.TDS.Cess,
                annualTaxLiability: result.TDS.AnnualProjectedTax,
                ytdTdsDeducted: 0m,
                remainingMonthsInFy: run.PayPeriod.MonthsRemainingInFiscalYear(),
                tdsThisMonth: result.TDS.MonthlyTDS,
                hasPanOverride: result.TDS.HasPanOverride,
                createdBy: req.ActorId), ct);

            applied++;
        }

        // Update run summary once with final state of all active employees
        var activeEmployees = allPayrunEmployees.Where(e => e.Status == PayrunEmployeeStatus.Active).ToList();
        run.UpdateFinancialSummary(
            payrollCost: activeEmployees.Sum(e => e.GrossPay + e.EmployerPf + e.EmployerEsi),
            totalNetPay: activeEmployees.Sum(e => e.NetPay),
            totalEmployerPf: activeEmployees.Sum(e => e.EmployerPf),
            totalEmployerEsi: activeEmployees.Sum(e => e.EmployerEsi),
            totalTds: activeEmployees.Sum(e => e.TdsAmount),
            totalPt: activeEmployees.Sum(e => e.PtAmount),
            employeeCount: activeEmployees.Count,
            actorId: req.ActorId);
        runRepo.Update(run);

        if (applied > 0)
            await uow.SaveChangesAsync(ct);

        return new ImportResult(applied, errors);
    }
}
