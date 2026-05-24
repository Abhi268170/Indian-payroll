using Payroll.Domain.Common;

namespace Payroll.Domain.Entities;

public sealed class LwfStateConfig : AuditableEntity
{
    private LwfStateConfig() { }

    public string StateCode { get; private set; } = string.Empty;
    public DateOnly EffectiveDate { get; private set; }
    public decimal EmployeeAmount { get; private set; }
    public decimal EmployerAmount { get; private set; }
    public bool IsPercentageBased { get; private set; }  // true only for Haryana
    public decimal? EmployeeRate { get; private set; }
    public decimal? EmployerRate { get; private set; }
    public decimal? RateCapEmployee { get; private set; }
    public decimal? RateCapEmployer { get; private set; }
    public string Frequency { get; private set; } = string.Empty;  // Monthly | HalfYearly | Annual
    public int? DeductionMonth { get; private set; }    // for Annual: month of year (Dec=12)
    public int? DepositDueDay { get; private set; }
    public decimal? WageThreshold { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static LwfStateConfig Create(
        string stateCode,
        DateOnly effectiveDate,
        decimal employeeAmount,
        decimal employerAmount,
        bool isPercentageBased,
        decimal? employeeRate,
        decimal? employerRate,
        decimal? rateCapEmployee,
        decimal? rateCapEmployer,
        string frequency,
        int? deductionMonth,
        int? depositDueDay,
        decimal? wageThreshold,
        Guid createdBy) =>
        new()
        {
            StateCode = stateCode,
            EffectiveDate = effectiveDate,
            EmployeeAmount = employeeAmount,
            EmployerAmount = employerAmount,
            IsPercentageBased = isPercentageBased,
            EmployeeRate = employeeRate,
            EmployerRate = employerRate,
            RateCapEmployee = rateCapEmployee,
            RateCapEmployer = rateCapEmployer,
            Frequency = frequency,
            DeductionMonth = deductionMonth,
            DepositDueDay = depositDueDay,
            WageThreshold = wageThreshold,
            CreatedBy = createdBy,
        };

    public void Activate(Guid updatedBy)
    {
        IsActive = true;
        SetUpdated(updatedBy);
    }

    public void Deactivate(Guid updatedBy)
    {
        IsActive = false;
        SetUpdated(updatedBy);
    }
}
