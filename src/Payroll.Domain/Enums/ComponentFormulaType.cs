namespace Payroll.Domain.Enums;

public enum ComponentFormulaType
{
    Fixed,
    PercentOfBasic,
    PercentOfGross,
    PercentOfCTC,
    // Used only by the Fixed Allowance system component: value = CTC − Σ(all other components)
    ResidualCTC,
}
