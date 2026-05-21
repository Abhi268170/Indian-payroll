namespace Payroll.Engine.Inputs;

public sealed record EmployeeInput(
    Guid EmployeeId,
    string EmployeeCode,
    string WorkStateCode,
    bool EpfEnabled,
    bool IsESIExempt,
    bool IsPWD,
    decimal MonthlyCTC,
    IReadOnlyList<SalaryComponentInput> Components,
    decimal LOPDays,
    decimal WorkingDaysInMonth,
    decimal VPFAmount,
    // YTD from prior employer for mid-year joiners
    decimal PriorEmployerYTDTaxableIncome,
    decimal PriorEmployerYTDTDSDeducted,
    decimal PriorEmployerYTDPF,
    // Half-year position for HalfYearlySplit PT states (e.g. Kerala).
    // MonthIndex == TotalMonths means last month → absorb rounding remainder.
    int HalfYearMonthIndex,
    int HalfYearTotalMonths,
    decimal BasicWage = 0m,
    bool GratuityEnabled = true,
    bool HasPan = true,
    // YTD from current employer (approved runs this FY in this system)
    decimal CurrentEmployerYTDGross = 0m,
    decimal CurrentEmployerYTDTDSDeducted = 0m);
