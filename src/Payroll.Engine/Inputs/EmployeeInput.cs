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
    decimal PriorEmployerYTDPF);
