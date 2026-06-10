namespace Payroll.Engine.Outputs;

public sealed record PFResult(
    decimal EmployeeContribution,
    decimal VPFContribution,
    decimal EPFEmployerContribution,
    decimal EPSEmployerContribution,
    bool IsExempt,
    // Employer-side statutory charges (not part of employee net pay).
    decimal EdliCharge = 0m,
    decimal AdminCharge = 0m);
