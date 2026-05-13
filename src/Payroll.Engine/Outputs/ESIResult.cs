namespace Payroll.Engine.Outputs;

public sealed record ESIResult(
    decimal EmployeeContribution,
    decimal EmployerContribution,
    bool IsExempt);
