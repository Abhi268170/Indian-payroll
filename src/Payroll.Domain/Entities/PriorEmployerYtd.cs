using Payroll.Domain.Common;

namespace Payroll.Domain.Entities;

public sealed class PriorEmployerYtd : AuditableEntity
{
    private PriorEmployerYtd() { }

    public Guid EmployeeId { get; private set; }
    public int FinancialYear { get; private set; }
    public string EmployerName { get; private set; } = string.Empty;
    public DateOnly PeriodFrom { get; private set; }
    public DateOnly PeriodTo { get; private set; }
    public decimal GrossSalary { get; private set; }
    public decimal StandardDeductionClaimed { get; private set; }
    public decimal ProfessionalTaxPaid { get; private set; }
    public decimal TdsDeducted { get; private set; }
    public decimal OtherIncome { get; private set; }

    public static PriorEmployerYtd Create(
        Guid employeeId,
        int financialYear,
        string employerName,
        DateOnly periodFrom,
        DateOnly periodTo,
        decimal grossSalary,
        decimal standardDeductionClaimed,
        decimal professionalTaxPaid,
        decimal tdsDeducted,
        decimal otherIncome,
        Guid createdBy) => new()
        {
            EmployeeId = employeeId,
            FinancialYear = financialYear,
            EmployerName = employerName,
            PeriodFrom = periodFrom,
            PeriodTo = periodTo,
            GrossSalary = grossSalary,
            StandardDeductionClaimed = standardDeductionClaimed,
            ProfessionalTaxPaid = professionalTaxPaid,
            TdsDeducted = tdsDeducted,
            OtherIncome = otherIncome,
            CreatedBy = createdBy
        };

    public void Update(
        string employerName,
        DateOnly periodFrom,
        DateOnly periodTo,
        decimal grossSalary,
        decimal standardDeductionClaimed,
        decimal professionalTaxPaid,
        decimal tdsDeducted,
        decimal otherIncome,
        Guid updatedBy)
    {
        EmployerName = employerName;
        PeriodFrom = periodFrom;
        PeriodTo = periodTo;
        GrossSalary = grossSalary;
        StandardDeductionClaimed = standardDeductionClaimed;
        ProfessionalTaxPaid = professionalTaxPaid;
        TdsDeducted = tdsDeducted;
        OtherIncome = otherIncome;
        SetUpdated(updatedBy);
    }
}
