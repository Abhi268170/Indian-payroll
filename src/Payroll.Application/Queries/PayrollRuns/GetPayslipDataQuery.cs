using MediatR;
using Payroll.Application.DTOs;
using Payroll.Application.Interfaces;
using Payroll.Application.Services;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.PayrollRuns;

public record GetPayslipDataQuery(Guid PayrollRunId, Guid EmployeeId) : IRequest<PayslipData>;

public sealed class GetPayslipDataHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IPayrunComponentBreakdownRepository breakdownRepo,
    IEmployeeRepository employeeRepo,
    IDesignationRepository designationRepo,
    IDepartmentRepository departmentRepo,
    IOrgProfileRepository orgProfileRepo,
    ISalaryComponentRepository componentRepo,
    IEmployeeExitRepository exitRepo,
    IEncryptionService encryption)
    : IRequestHandler<GetPayslipDataQuery, PayslipData>
{
    public async Task<PayslipData> Handle(GetPayslipDataQuery req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.PayrollRunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.PayrollRunId} not found.");

        var payrunEmp = await payrunEmployeeRepo.GetByRunAndEmployeeAsync(req.PayrollRunId, req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not in payroll run {req.PayrollRunId}.");

        var employee = await employeeRepo.GetByIdAsync(req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not found.");

        var breakdowns = await breakdownRepo.GetByRunAndEmployeeAsync(req.PayrollRunId, req.EmployeeId, ct);

        var allComponents = await componentRepo.ListByTenantAsync(run.TenantId, ct: ct);
        var deductionIds = allComponents
            .Where(c => c.Category == ComponentCategory.Deduction)
            .Select(c => c.Id)
            .ToHashSet();

        var designation = await designationRepo.GetByIdAsync(employee.DesignationId, ct);
        var department = await departmentRepo.GetByIdAsync(employee.DepartmentId, ct);
        var orgProfile = await orgProfileRepo.GetAsync(ct);

        // YTD: sum across all Paid runs in the same fiscal year
        int fiscalYear = run.PayPeriod.FiscalYear;
        var ytdRunIds = await runRepo.GetPaidIdsForFiscalYearAsync(fiscalYear, ct);
        var ytdPayrunEmps = await payrunEmployeeRepo.GetByEmployeeAndRunIdsAsync(req.EmployeeId, ytdRunIds, ct);
        var ytdBreakdowns = await breakdownRepo.GetByEmployeeAndRunIdsAsync(req.EmployeeId, ytdRunIds, ct);

        decimal ytdGross = ytdPayrunEmps.Sum(e => e.GrossPay);
        decimal ytdNetPay = ytdPayrunEmps.Sum(e => e.NetPay);
        decimal ytdTds = ytdPayrunEmps.Sum(e => e.TdsOverrideAmount ?? e.TdsAmount);
        decimal ytdPf = ytdPayrunEmps.Sum(e => e.EmployeePf);

        // Build per-component YTD map from YTD breakdowns (keyed by ComponentCode)
        var ytdByComponent = ytdBreakdowns
            .GroupBy(b => b.ComponentCode)
            .ToDictionary(g => g.Key, g => g.Sum(b => b.ProratedAmount));

        var components = breakdowns
            .Where(b => b.ShowInPayslip)
            .Select(b => new PayslipComponentDto(
            b.ComponentCode,
            b.ComponentName,
            b.ProratedAmount,
            ytdByComponent.GetValueOrDefault(b.ComponentCode, 0m),
            IsEarning: b.SalaryComponentId is null
                ? false  // reimbursement — show in deductions/reimbursements column
                : !deductionIds.Contains(b.SalaryComponentId.Value)))
            .ToList();

        // FnF context: an exit may exist for this employee. PayslipPdfGenerator
        // branches on IsFinalSettlement to render the Exit Details block.
        bool isFnf = run.Type == PayrollRunType.FinalSettlement || run.Type == PayrollRunType.BulkFinalSettlement;
        var exit = isFnf ? await exitRepo.GetActiveByEmployeeAsync(req.EmployeeId, ct) : null;

        string maskedBankAccount = MaskBankAccount(employee.EncryptedBankAccount, encryption);
        string? ifscCode = employee.EncryptedIFSC is not null
            ? encryption.Decrypt(employee.EncryptedIFSC)
            : null;

        string periodLabel = new DateTime(run.PayPeriod.Year, run.PayPeriod.Month, 1).ToString("MMMM yyyy");
        string netPayInWords = IndianNumberToWords.Convert(payrunEmp.NetPay);

        string companyAddress = BuildAddress(orgProfile?.AddressLine1, orgProfile?.AddressLine2);

        return new PayslipData(
            PayrollRunId: req.PayrollRunId,
            EmployeeId: req.EmployeeId,
            EmployeeCode: employee.EmployeeCode,
            EmployeeName: employee.FullName,
            Designation: designation?.Name ?? string.Empty,
            Department: department?.Name ?? string.Empty,
            CompanyName: orgProfile?.CompanyName ?? string.Empty,
            CompanyAddress: string.IsNullOrEmpty(companyAddress) ? null : companyAddress,
            PayPeriodYear: run.PayPeriod.Year,
            PayPeriodMonth: run.PayPeriod.Month,
            PeriodLabel: periodLabel,
            PayDay: run.PayDay,
            MonthlyCTC: payrunEmp.MonthlyCTC,
            GrossPay: payrunEmp.GrossPay,
            NetPay: payrunEmp.NetPay,
            NetPayInWords: netPayInWords,
            EmployeePf: payrunEmp.EmployeePf,
            EmployerPf: payrunEmp.EmployerPf,
            EmployeeEsi: payrunEmp.EmployeeEsi,
            EmployerEsi: payrunEmp.EmployerEsi,
            PtAmount: payrunEmp.PtAmount,
            LwfEmployeeAmount: payrunEmp.LwfEmployeeAmount,
            TdsAmount: payrunEmp.TdsOverrideAmount ?? payrunEmp.TdsAmount,
            GratuityAmount: payrunEmp.GratuityAmount,
            YtdGross: ytdGross,
            YtdNetPay: ytdNetPay,
            YtdTds: ytdTds,
            YtdPf: ytdPf,
            MaskedBankAccount: maskedBankAccount,
            BankName: employee.BankName,
            IfscCode: ifscCode,
            Components: components,
            IsFinalSettlement: isFnf,
            LastWorkingDay: exit?.LastWorkingDay,
            ExitReason: exit?.Reason.ToString(),
            TenureLabel: exit is null ? null : employee.TenureAt(exit.LastWorkingDay).ToString(),
            ExitNotes: exit?.Notes);
    }

    private static string MaskBankAccount(string? encryptedBankAccount, IEncryptionService encryption)
    {
        if (encryptedBankAccount is null) return "XXXX";
        string account = encryption.Decrypt(encryptedBankAccount);
        if (account.Length <= 4) return new string('X', account.Length);
        return new string('X', account.Length - 4) + account[^4..];
    }

    private static string BuildAddress(string? line1, string? line2) =>
        string.Join(", ", new[] { line1, line2 }.Where(s => !string.IsNullOrEmpty(s)));
}
