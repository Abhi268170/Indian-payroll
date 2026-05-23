namespace Payroll.Engine.Inputs;

public sealed record SalaryComponentInput(
    Guid ComponentId,
    string Code,
    decimal Amount,
    bool IsTaxable,
    bool ConsiderForEpf = false,
    bool ConsiderForEsi = false,
    bool CalculateOnProRata = true,
    bool IsFlat = false,
    bool ShowInPayslip = true);
