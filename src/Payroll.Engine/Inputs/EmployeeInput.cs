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
    // Voluntary PF as a PERCENTAGE of the employee PF wage (e.g. 5 = 5%), never a rupee amount.
    decimal VPFPercent,
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
    decimal CurrentEmployerYTDTDSDeducted = 0m,
    // Sum of prorated taxable component amounts from approved runs this FY.
    // Distinct from gross because non-taxable components inflate gross but must not
    // inflate the annualised taxable projection used by TDS.
    decimal CurrentEmployerYTDTaxable = 0m,
    // Needed for gender-split PT slabs (e.g. Maharashtra). "Male"/"Female"/null.
    string? Gender = null,
    // ESI rule: once covered at the start of a contribution period (Apr–Sep / Oct–Mar),
    // the employee contributes until the period ends even if wages cross the limit.
    // Application sets this when the employee contributed earlier in the current period.
    bool EsiContinueInPeriod = false,
    // Per-employee statutory exemptions (e.g. PT disability exemption).
    bool PtApplicable = true,
    bool LwfApplicable = true);
