namespace Payroll.Engine.Inputs;

public sealed record SalaryComponentInput(
    Guid ComponentId,
    string Code,
    decimal Amount,
    bool IsTaxable);
