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
    bool ShowInPayslip = true,
    // One-time amounts (bonus, commission, salary-revision arrears) are paid and
    // taxed in this month only. They must NOT be multiplied by MonthsRemainingInFY
    // when projecting annual income for TDS — doing so over-taxes a non-recurring
    // payment. Recurring components project ×N; one-time components add ×1.
    bool IsOneTime = false);
