using MediatR;
using Payroll.Application.DTOs;
using Payroll.Application.Services;
using Payroll.Application.Utilities;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.PayrollRuns;

public record BulkImportOneTimeEarningsCommand(Guid RunId, string CsvContent, Guid ActorId) : IRequest<ImportResult>;

public sealed class BulkImportOneTimeEarningsCommandHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IPayrunComponentBreakdownRepository breakdownRepo,
    IEmployeeRepository employeeRepo,
    ISalaryComponentRepository componentRepo,
    Payroll.Application.Services.IPayrollRecomputeService recomputeService,
    Payroll.Application.Services.IPayrollCostCalculator costCalculator,
    IUnitOfWork uow)
    : IRequestHandler<BulkImportOneTimeEarningsCommand, ImportResult>
{
    public async Task<ImportResult> Handle(BulkImportOneTimeEarningsCommand req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        if (run.Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException("Variable inputs can only be changed on a Draft payroll run.");

        IReadOnlyList<string[]> rows = CsvParser.Parse(req.CsvContent);

        // Batch-load employees
        List<string> employeeCodes = rows
            .Select(r => r.Length > 0 ? r[0] : string.Empty)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        IReadOnlyList<Employee> employees = await employeeRepo.GetManyByCodesAsync(employeeCodes, ct);
        Dictionary<string, Employee> empByCode = employees.ToDictionary(e => e.EmployeeCode, StringComparer.OrdinalIgnoreCase);

        // Batch-load payrun employees
        IReadOnlyList<PayrunEmployee> allPayrunEmployees = await payrunEmployeeRepo.GetByRunIdAsync(req.RunId, ct);
        Dictionary<Guid, PayrunEmployee> payrunEmpById = allPayrunEmployees.ToDictionary(e => e.EmployeeId);

        // Load active earnings + deductions — both eligible for one-time import
        IReadOnlyList<SalaryComponent> activeEarnings = await componentRepo.ListActiveEarningsAsync(run.TenantId, ct);
        List<SalaryComponent> activeDeductions = await componentRepo.ListByTenantAsync(run.TenantId, ComponentCategory.Deduction, ct);
        activeDeductions = activeDeductions.Where(c => c.IsActive && !c.IsSystemComponent).ToList();

        Dictionary<string, SalaryComponent> componentByCode = activeEarnings
            .Concat(activeDeductions)
            .ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);

        int applied = 0;
        List<ImportRowError> errors = [];
        List<PayrunComponentBreakdown> newBreakdowns = [];

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            string[] row = rows[rowIndex];
            int displayRow = rowIndex + 2;

            string employeeCode = row.Length > 0 ? row[0] : string.Empty;
            string componentCode = row.Length > 1 ? row[1] : string.Empty;
            string amountRaw = row.Length > 2 ? row[2] : string.Empty;

            if (string.IsNullOrWhiteSpace(employeeCode))
            {
                errors.Add(new(displayRow, string.Empty, "Employee Code is required."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(componentCode))
            {
                errors.Add(new(displayRow, employeeCode, "Component Code is required."));
                continue;
            }

            if (!decimal.TryParse(amountRaw, out decimal amount) || amount <= 0)
            {
                errors.Add(new(displayRow, employeeCode, "Amount must be a positive number."));
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

            if (!componentByCode.TryGetValue(componentCode, out SalaryComponent? component))
            {
                errors.Add(new(displayRow, employeeCode, $"Component '{componentCode}' was not found, is inactive, or cannot be imported here."));
                continue;
            }

            if (!component.IsOneTime)
            {
                errors.Add(new(displayRow, employeeCode, $"Component '{componentCode}' is not flagged as one-time. Mark it as one-time in Settings or use the salary structure to apply recurring components."));
                continue;
            }

            bool isDeduction = component.Category == ComponentCategory.Deduction;

            var breakdown = PayrunComponentBreakdown.Create(
                payrollRunId: run.Id,
                employeeId: employee.Id,
                tenantId: run.TenantId,
                salaryComponentId: component.Id,
                componentCode: component.Code,
                componentName: component.NameInPayslip,
                fullAmount: amount,
                proratedAmount: amount,
                isOneTimeEarning: true,
                isTaxable: !isDeduction && (component.IsTaxable ?? true),
                considerForEpf: !isDeduction && (component.ConsiderForEpf ?? false),
                considerForEsi: !isDeduction && (component.ConsiderForEsi ?? false),
                calculateOnProRata: false,
                epfInclusionRule: component.EpfInclusionRule ?? EpfInclusionRule.Always,
                showInPayslip: component.ShowInPayslip ?? true);
            newBreakdowns.Add(breakdown);
            applied++;
        }

        // Persist all new breakdowns before recomputing so the service sees them.
        await breakdownRepo.AddRangeAsync(newBreakdowns, ct);
        await uow.SaveChangesAsync(ct);

        // Recompute every affected employee once, then refresh payrun_employee rows.
        HashSet<Guid> affectedEmployeeIds = newBreakdowns.Select(b => b.EmployeeId).ToHashSet();
        foreach (Guid empId in affectedEmployeeIds)
        {
            RecomputeResult recompute = await recomputeService.RecomputeEmployeeAsync(req.RunId, empId, ct);
            PayrunEmployee payrunEmp = payrunEmpById[empId];
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
        }

        // Update run summary once
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
