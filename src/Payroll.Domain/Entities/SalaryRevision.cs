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

    public void Apply(Guid updatedBy)
    {
        Status = SalaryRevisionStatus.Applied;
        SetUpdated(updatedBy);
    }
}
