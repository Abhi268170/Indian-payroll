using Payroll.Domain.Common;

namespace Payroll.Domain.Entities;

public sealed class PayrunComponentBreakdown : AuditableEntity
{
    private PayrunComponentBreakdown() { }

    public Guid PayrollRunId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? SalaryComponentId { get; private set; }
    public string ComponentCode { get; private set; } = default!;
    public string ComponentName { get; private set; } = default!;
    public decimal FullAmount { get; private set; }
    public decimal ProratedAmount { get; private set; }
    public bool IsOneTimeEarning { get; private set; }
    public bool ConsiderForEpf { get; private set; }
    public bool ShowInPayslip { get; private set; }

    public static PayrunComponentBreakdown Create(
        Guid payrollRunId,
        Guid employeeId,
        Guid tenantId,
        Guid? salaryComponentId,
        string componentCode,
        string componentName,
        decimal fullAmount,
        decimal proratedAmount,
        bool isOneTimeEarning,
        bool considerForEpf = false,
        bool showInPayslip = true) =>
        new()
        {
            PayrollRunId = payrollRunId,
            EmployeeId = employeeId,
            TenantId = tenantId,
            SalaryComponentId = salaryComponentId,
            ComponentCode = componentCode,
            ComponentName = componentName,
            FullAmount = fullAmount,
            ProratedAmount = proratedAmount,
            IsOneTimeEarning = isOneTimeEarning,
            ConsiderForEpf = considerForEpf,
            ShowInPayslip = showInPayslip
        };

    public void UpdateAmounts(decimal fullAmount, decimal proratedAmount)
    {
        FullAmount = fullAmount;
        ProratedAmount = proratedAmount;
    }
}
