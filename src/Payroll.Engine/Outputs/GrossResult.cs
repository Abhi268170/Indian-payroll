namespace Payroll.Engine.Outputs;

public sealed record GrossResult(
    decimal GrossWage,
    decimal PFWage,       // LOP-prorated PF wage (actual earned)
    decimal FullPFWage,   // non-prorated PF wage from salary structure
    decimal AnnualProjectedGross,
    decimal LOPDeduction,
    decimal ArrearAmount,
    IReadOnlyList<ComponentAmountResult> ComponentBreakdown);
