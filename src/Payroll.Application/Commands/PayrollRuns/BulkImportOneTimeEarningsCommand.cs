using MediatR;
using Payroll.Application.DTOs;
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

        // Batch-load all active earnings for tenant — build code → component dict
        IReadOnlyList<SalaryComponent> activeEarnings = await componentRepo.ListActiveEarningsAsync(run.TenantId, ct);
        Dictionary<string, SalaryComponent> componentByCode = activeEarnings.ToDictionary(
            c => c.Code, StringComparer.OrdinalIgnoreCase);

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

            if (!componentByCode.TryGetValue(componentCode, out SalaryComponent? component))
            {
                errors.Add(new(displayRow, employeeCode, $"Component '{componentCode}' not found or not an active earning."));
                continue;
            }

            // Create breakdown row
            var breakdown = PayrunComponentBreakdown.Create(
                run.Id, employee.Id, run.TenantId,
                component.Id, component.Code, component.Name ?? component.Code,
                amount, amount, isOneTimeEarning: true);
            newBreakdowns.Add(breakdown);

            // Update payrun employee aggregates
            payrunEmp.UpdateComputedAmounts(
                grossPay: payrunEmp.GrossPay + amount,
                netPay: payrunEmp.NetPay + amount,
                taxesAmount: payrunEmp.TaxesAmount,
                benefitsAmount: payrunEmp.BenefitsAmount,
                reimbursementsAmount: payrunEmp.ReimbursementsAmount,
                employeePf: payrunEmp.EmployeePf,
                employerPf: payrunEmp.EmployerPf,
                employeeEsi: payrunEmp.EmployeeEsi,
                employerEsi: payrunEmp.EmployerEsi,
                ptAmount: payrunEmp.PtAmount,
                tdsAmount: payrunEmp.TdsAmount,
                lwfEmployeeAmount: payrunEmp.LwfEmployeeAmount,
                lwfEmployerAmount: payrunEmp.LwfEmployerAmount,
                gratuityAmount: payrunEmp.GratuityAmount,
                epsAmount: payrunEmp.EpsAmount,
                monthlyCTC: payrunEmp.MonthlyCTC,
                actorId: req.ActorId);

            payrunEmployeeRepo.Update(payrunEmp);
            applied++;
        }

        // Batch add all new breakdowns
        foreach (var bd in newBreakdowns)
            await breakdownRepo.AddAsync(bd, ct);

        // Update run summary once
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
