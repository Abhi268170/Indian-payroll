using Payroll.Domain.Common;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Entities;

public sealed class PaySchedule : AuditableEntity
{
    private PaySchedule() { }

    public WorkWeekDay WorkWeekDays { get; private set; }
    public SalaryCalculationMethod SalaryCalculationMethod { get; private set; }
    public int? FixedWorkingDaysPerMonth { get; private set; }
    public PayDateType PayDateType { get; private set; }
    public int? PayDateDay { get; private set; }

    // First pay period — month (1-12) and year for the first payroll run
    public int? FirstPayPeriodMonth { get; private set; }
    public int? FirstPayPeriodYear { get; private set; }

    /// <summary>
    /// Set to true after the first pay run is finalised.
    /// Once locked, WorkWeekDays and SalaryCalculationMethod cannot be changed
    /// because they affect historical proration calculations.
    /// PayDateType and PayDateDay remain editable at all times.
    /// </summary>
    public bool IsLockedAfterPayrun { get; private set; }

    public static PaySchedule Create(
        WorkWeekDay workWeekDays,
        SalaryCalculationMethod salaryCalculationMethod,
        int? fixedWorkingDaysPerMonth,
        PayDateType payDateType,
        int? payDateDay,
        int? firstPayPeriodMonth,
        int? firstPayPeriodYear,
        Guid createdBy) =>
        new()
        {
            WorkWeekDays = workWeekDays,
            SalaryCalculationMethod = salaryCalculationMethod,
            FixedWorkingDaysPerMonth = fixedWorkingDaysPerMonth,
            PayDateType = payDateType,
            PayDateDay = payDateDay,
            FirstPayPeriodMonth = firstPayPeriodMonth,
            FirstPayPeriodYear = firstPayPeriodYear,
            CreatedBy = createdBy,
        };

    public void Update(
        WorkWeekDay workWeekDays,
        SalaryCalculationMethod salaryCalculationMethod,
        int? fixedWorkingDaysPerMonth,
        PayDateType payDateType,
        int? payDateDay,
        int? firstPayPeriodMonth,
        int? firstPayPeriodYear,
        Guid updatedBy)
    {
        if (IsLockedAfterPayrun)
        {
            if (workWeekDays != WorkWeekDays)
                throw new DomainException(
                    "Work week cannot be changed after a payroll run has been processed.");

            if (salaryCalculationMethod != SalaryCalculationMethod)
                throw new DomainException(
                    "Salary calculation method cannot be changed after a payroll run has been processed.");
        }

        WorkWeekDays = workWeekDays;
        SalaryCalculationMethod = salaryCalculationMethod;
        FixedWorkingDaysPerMonth = fixedWorkingDaysPerMonth;
        PayDateType = payDateType;
        PayDateDay = payDateDay;
        FirstPayPeriodMonth = firstPayPeriodMonth;
        FirstPayPeriodYear = firstPayPeriodYear;
        SetUpdated(updatedBy);
    }

    public void LockAfterPayrun()
    {
        IsLockedAfterPayrun = true;
    }
}
