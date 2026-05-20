namespace Payroll.Engine.Outputs;

public sealed record PFResult(
    decimal EmployeeContribution,
    decimal VPFContribution,
    decimal EPFEmployerContribution,
    decimal EPSEmployerContribution,
    bool IsExempt);
