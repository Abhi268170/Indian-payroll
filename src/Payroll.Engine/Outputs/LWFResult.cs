namespace Payroll.Engine.Outputs;

public sealed record LWFResult(decimal EmployeeAmount, decimal EmployerAmount, bool IsExempt);
