using Payroll.Domain.Common;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Entities;

public sealed class SalaryRevision : AuditableEntity
{
    private SalaryRevision() { }

    public Guid EmployeeId { get; private set; }
    public Guid TenantId { get; private set; }
    public decimal PreviousAnnualCTC { get; private set; }
    public decimal NewAnnualCTC { get; private set; }
    public int EffectiveFromMonth { get; private set; }
    public int EffectiveFromYear { get; private set; }
    public int PayoutMonth { get; private set; }
    public int PayoutYear { get; private set; }
    public Guid? SalaryStructureTemplateId { get; private set; }
    public string? Notes { get; private set; }
    public SalaryRevisionStatus Status { get; private set; }

    // Set on Apply: the backdated EmployeeSalaryStructure this revision created.
    public Guid? ResultingSalaryStructureId { get; private set; }

    // Set when a payout run carrying this revision's arrears is finalised; cleared
    // if that run is later rejected/deleted so a re-run re-injects. Injection skips
    // revisions that already have a value here.
    public Guid? ArrearPaidRunId { get; private set; }

    // Per-component override snapshot captured at creation, used to reproduce the
    // backdated structure on apply.
    public ICollection<SalaryRevisionComponentOverride> ComponentOverrides { get; private set; }
        = new List<SalaryRevisionComponentOverride>();

    public static SalaryRevision Create(
        Guid employeeId,
        Guid tenantId,
        decimal previousAnnualCTC,
        decimal newAnnualCTC,
        int effectiveFromMonth,
        int effectiveFromYear,
        int payoutMonth,
        int payoutYear,
        Guid? salaryStructureTemplateId,
        string? notes,
        Guid createdBy) => new()
        {
            EmployeeId = employeeId,
            TenantId = tenantId,
            PreviousAnnualCTC = previousAnnualCTC,
            NewAnnualCTC = newAnnualCTC,
            EffectiveFromMonth = effectiveFromMonth,
            EffectiveFromYear = effectiveFromYear,
            PayoutMonth = payoutMonth,
            PayoutYear = payoutYear,
            SalaryStructureTemplateId = salaryStructureTemplateId,
            Notes = notes,
            Status = SalaryRevisionStatus.Pending,
            CreatedBy = createdBy
        };

    public void AddOverride(SalaryRevisionComponentOverride o) =>
        ComponentOverrides.Add(o);

    public void Apply(Guid resultingSalaryStructureId, Guid updatedBy)
    {
        Status = SalaryRevisionStatus.Applied;
        ResultingSalaryStructureId = resultingSalaryStructureId;
        SetUpdated(updatedBy);
    }

    // Payout run carrying this revision's arrears has been finalised.
    public void MarkArrearPaid(Guid payoutRunId, Guid updatedBy)
    {
        ArrearPaidRunId = payoutRunId;
        SetUpdated(updatedBy);
    }

    // Payout run was rejected/deleted — allow a future run to re-inject the arrears.
    public void ClearArrearPaid(Guid updatedBy)
    {
        ArrearPaidRunId = null;
        SetUpdated(updatedBy);
    }
}
