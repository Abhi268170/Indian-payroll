using Payroll.Domain.Common;
using Payroll.Domain.Enums;

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

    // Statutory flags frozen at breakdown-creation time so engine recompute is
    // deterministic even if the linked SalaryComponent is edited mid-period.
    public bool IsTaxable { get; private set; }
    public bool ConsiderForEpf { get; private set; }
    public bool ConsiderForEsi { get; private set; }
    public bool CalculateOnProRata { get; private set; }
    public EpfInclusionRule EpfInclusionRule { get; private set; }
    public bool ShowInPayslip { get; private set; }
    // True for employer-borne benefit rows (health insurance, NPS employer match,
    // etc.). These are part of CTC, surfaced on the payslip in a separate
    // "Employer benefits" section, and never roll into gross / net / PF / ESI /
    // taxable wage. Engine never touches them; persisted for audit + display only.
    public bool IsBenefit { get; private set; }

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
        bool isTaxable = true,
        bool considerForEpf = false,
        bool considerForEsi = false,
        bool calculateOnProRata = true,
        EpfInclusionRule epfInclusionRule = EpfInclusionRule.Always,
        bool showInPayslip = true,
        bool isBenefit = false) =>
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
            IsTaxable = isTaxable,
            ConsiderForEpf = considerForEpf,
            ConsiderForEsi = considerForEsi,
            CalculateOnProRata = calculateOnProRata,
            EpfInclusionRule = epfInclusionRule,
            ShowInPayslip = showInPayslip,
            IsBenefit = isBenefit
        };

    public void UpdateAmounts(decimal fullAmount, decimal proratedAmount)
    {
        FullAmount = fullAmount;
        ProratedAmount = proratedAmount;
    }
}
