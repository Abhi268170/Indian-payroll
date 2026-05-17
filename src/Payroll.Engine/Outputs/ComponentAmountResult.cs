namespace Payroll.Engine.Outputs;

public sealed record ComponentAmountResult(
    Guid ComponentId,
    string Code,
    decimal FullAmount,
    decimal ProratedAmount);
