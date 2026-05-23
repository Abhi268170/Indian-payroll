using MediatR;
using Payroll.Application.DTOs;
using Payroll.Application.Utilities;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.PayrollRuns;

public record BulkImportReimbursementsCommand(Guid RunId, string CsvContent, Guid ActorId) : IRequest<ImportResult>;

public sealed class BulkImportReimbursementsCommandHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IEmployeeRepository employeeRepo,
    IUnitOfWork uow)
    : IRequestHandler<BulkImportReimbursementsCommand, ImportResult>
{
    public async Task<ImportResult> Handle(BulkImportReimbursementsCommand req, CancellationToken ct)
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

        int applied = 0;
        List<ImportRowError> errors = [];

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            string[] row = rows[rowIndex];
            int displayRow = rowIndex + 2;

            string employeeCode = row.Length > 0 ? row[0] : string.Empty;
            string reportNumber = row.Length > 1 ? row[1] : string.Empty;
            string amountRaw = row.Length > 2 ? row[2] : string.Empty;

            if (string.IsNullOrWhiteSpace(employeeCode))
            {
                errors.Add(new(displayRow, string.Empty, "Employee Code is required."));
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

            // Update reimbursements aggregate + net pay
            payrunEmp.UpdateComputedAmounts(
                grossPay: payrunEmp.GrossPay,
                netPay: payrunEmp.NetPay + amount,
                taxesAmount: payrunEmp.TaxesAmount,
                benefitsAmount: payrunEmp.BenefitsAmount,
                reimbursementsAmount: payrunEmp.ReimbursementsAmount + amount,
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
