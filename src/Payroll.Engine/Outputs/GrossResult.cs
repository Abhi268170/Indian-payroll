namespace Payroll.Engine.Outputs;

public sealed record GrossResult(
    decimal GrossWage,
    decimal PFWage,
    decimal AnnualProjectedGross,
    decimal LOPDeduction,
    decimal ArrearAmount,
    IReadOnlyList<ComponentAmountResult> ComponentBreakdown);
