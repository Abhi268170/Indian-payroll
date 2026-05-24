using Payroll.Domain.Common;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Entities;

public sealed class PayrunEmployee : AuditableEntity
{
    private PayrunEmployee() { }

    public Guid PayrollRunId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid TenantId { get; private set; }
    public PayrunEmployeeStatus Status { get; private set; }

    // Pay period basis
    public int BaseDays { get; private set; }
    public int LopDays { get; private set; }
    public int ActualPayableDays { get; private set; }

    // Computed amounts
    public decimal GrossPay { get; private set; }
    public decimal NetPay { get; private set; }
    public decimal TaxesAmount { get; private set; }
    public decimal BenefitsAmount { get; private set; }
    public decimal ReimbursementsAmount { get; private set; }

    // Statutory deductions (employee share)
    public decimal EmployeePf { get; private set; }
    public decimal EmployerPf { get; private set; }
    public decimal EmployeeEsi { get; private set; }
    public decimal EmployerEsi { get; private set; }
    public decimal PtAmount { get; private set; }
    public decimal TdsAmount { get; private set; }
    public decimal LwfEmployeeAmount { get; private set; }
    public decimal LwfEmployerAmount { get; private set; }
    public decimal GratuityAmount { get; private set; }
    public decimal EpsAmount { get; private set; }
    public decimal MonthlyCTC { get; private set; }

    // TDS override
    public decimal? TdsOverrideAmount { get; private set; }
    public string? TdsOverrideReason { get; private set; }

    // Skip / withhold
    public string? SkipReason { get; private set; }
    public bool IsWithheld { get; private set; }

    // Payment
    public PaymentMode? PaymentModeOverride { get; private set; }

    // Set when this row sits in a FinalSettlement or BulkFinalSettlement run.
    // Links back to the EmployeeExit that scheduled this employee for the FnF.
    public Guid? EmployeeExitId { get; private set; }

    public static PayrunEmployee Create(
        Guid payrollRunId,
        Guid employeeId,
        Guid tenantId,
        int baseDays,
        Guid createdBy,
        Guid? employeeExitId = null) =>
        new()
        {
            PayrollRunId = payrollRunId,
            EmployeeId = employeeId,
            TenantId = tenantId,
            Status = PayrunEmployeeStatus.Active,
            BaseDays = baseDays,
            LopDays = 0,
            ActualPayableDays = baseDays,
            EmployeeExitId = employeeExitId,
            CreatedBy = createdBy
        };

    public void UpdateComputedAmounts(
        decimal grossPay,
        decimal netPay,
        decimal taxesAmount,
        decimal benefitsAmount,
        decimal reimbursementsAmount,
        decimal employeePf,
        decimal employerPf,
        decimal employeeEsi,
        decimal employerEsi,
        decimal ptAmount,
        decimal tdsAmount,
        decimal lwfEmployeeAmount,
        decimal lwfEmployerAmount,
        decimal gratuityAmount,
        decimal epsAmount,
        decimal monthlyCTC,
        Guid actorId)
    {
        GrossPay = grossPay;
        NetPay = netPay;
        TaxesAmount = taxesAmount;
        BenefitsAmount = benefitsAmount;
        ReimbursementsAmount = reimbursementsAmount;
        EmployeePf = employeePf;
        EmployerPf = employerPf;
        EmployeeEsi = employeeEsi;
        EmployerEsi = employerEsi;
        PtAmount = ptAmount;
        TdsAmount = tdsAmount;
        LwfEmployeeAmount = lwfEmployeeAmount;
        LwfEmployerAmount = lwfEmployerAmount;
        GratuityAmount = gratuityAmount;
        EpsAmount = epsAmount;
        MonthlyCTC = monthlyCTC;
        SetUpdated(actorId);
    }

    public void SetLop(int lopDays, Guid actorId)
    {
        if (lopDays < 0)
            throw new InvalidOperationException("LOP days cannot be negative.");
        if (lopDays >= BaseDays)
            throw new InvalidOperationException($"LOP days ({lopDays}) must be less than base days ({BaseDays}).");

        LopDays = lopDays;
        ActualPayableDays = BaseDays - lopDays;
        SetUpdated(actorId);
    }

    public void SetTdsOverride(decimal amount, string reason, Guid actorId)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("TDS override reason is mandatory.");

        TdsOverrideAmount = amount;
        TdsOverrideReason = reason;
        SetUpdated(actorId);
    }

    public void ClearTdsOverride(Guid actorId)
    {
        TdsOverrideAmount = null;
        TdsOverrideReason = null;
        SetUpdated(actorId);
    }

    public void Skip(string reason, Guid actorId)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Skip reason is mandatory.");
        if (Status == PayrunEmployeeStatus.Skipped)
            throw new InvalidOperationException("Employee is already skipped.");

        Status = PayrunEmployeeStatus.Skipped;
        SkipReason = reason;
        SetUpdated(actorId);
    }

    public void UndoSkip(Guid actorId)
    {
        if (Status != PayrunEmployeeStatus.Skipped)
            throw new InvalidOperationException("Employee is not skipped.");

        Status = PayrunEmployeeStatus.Active;
        SkipReason = null;
        SetUpdated(actorId);
    }

    public void Withhold(Guid actorId)
    {
        Status = PayrunEmployeeStatus.Withheld;
        IsWithheld = true;
        SetUpdated(actorId);
    }

    public void ReleaseWithheld(Guid actorId)
    {
        if (!IsWithheld)
            throw new InvalidOperationException("Employee is not withheld.");

        Status = PayrunEmployeeStatus.Active;
        IsWithheld = false;
        SetUpdated(actorId);
    }
}
