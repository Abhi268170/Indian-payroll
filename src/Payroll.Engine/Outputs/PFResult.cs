namespace Payroll.Engine.Outputs;

public sealed record PFResult(
    decimal EmployeeContribution,
    decimal VPFContribution,
    decimal EPFEmployerContribution,
    decimal EPSEmployerContribution,
    decimal EDLIEmployerContribution,
    decimal EPFAdminContribution,
    bool IsExempt);
