namespace Payroll.Application.Services;

// WI-018 step 4 — pure arrear math.
//
// For each affected back month we know the OLD per-component amounts (full + the
// prorated value actually paid, read from that month's finalised PayrunComponentBreakdown)
// and the NEW per-component full amounts (resolved from the backdated structure via
// BuildComponentInputs). The arrear is the per-component difference of the NEW prorated
// amount minus the OLD prorated amount.
//
// Proration is reproduced from the OLD run's *realized factor* (oldProrated / oldFull)
// rather than recomputed from days. That factor already encodes everything the original
// run applied — LOP, the salary divisor (calendar vs fixed 26/30), mid-month joining,
// flat components (factor 1), and CalculateOnProRata=false (factor 1) — so the arrear
// shares the original run's exact basis without re-deriving any of it.
//
// Deterministic and I/O-free so the diff rules are unit-testable without a database.

public sealed record ArrearOldComponent(string Code, decimal ProratedAmount, decimal FullAmount);

public sealed record ArrearNewComponent(
    Guid ComponentId,
    string Code,
    string Name,
    decimal FullAmount,
    bool IsTaxable);

// One affected back month: the old amounts from its breakdown and the new full amounts.
public sealed record ArrearMonth(
    int Year,
    int Month,
    IReadOnlyList<ArrearOldComponent> Old,
    IReadOnlyList<ArrearNewComponent> New);

public sealed record ArrearLine(
    Guid ComponentId,
    string Code,
    string Name,
    decimal Amount,
    bool IsTaxable,
    bool ConsiderForEpf,
    bool ConsiderForEsi);

public sealed record ArrearComputation(
    IReadOnlyList<ArrearLine> Lines,
    decimal TotalArrear,
    decimal TotalTaxableArrear);

public static class SalaryArrearCalculator
{
    public static ArrearComputation Compute(IReadOnlyList<ArrearMonth> months)
    {
        // Accumulate per-component arrear across all months, keyed by component code
        // (codes are stable across structure versions; component ids may differ).
        Dictionary<string, ArrearLine> byCode = new Dictionary<string, ArrearLine>(StringComparer.OrdinalIgnoreCase);

        foreach (ArrearMonth m in months)
        {
            Dictionary<string, ArrearOldComponent> oldByCode = m.Old.ToDictionary(o => o.Code, StringComparer.OrdinalIgnoreCase);
            Dictionary<string, ArrearNewComponent> newByCode = m.New.ToDictionary(n => n.Code, StringComparer.OrdinalIgnoreCase);

            // The month's realized LOP factor, used only for components that exist in the
            // new structure but had no counterpart in the old run (so no per-component
            // factor is available). Derived from any old component that was actually
            // prorated; 1 when the month had no proration.
            decimal monthFactor = DeriveMonthFactor(m.Old);

            // Full outer join on code: a component may exist only in the new structure
            // (added → arrear is its full new prorated amount) or only in the old run
            // (removed → negative recovery).
            HashSet<string> codes = new HashSet<string>(oldByCode.Keys, StringComparer.OrdinalIgnoreCase);
            codes.UnionWith(newByCode.Keys);

            foreach (string code in codes)
            {
                oldByCode.TryGetValue(code, out ArrearOldComponent? oc);
                newByCode.TryGetValue(code, out ArrearNewComponent? nc);

                decimal newProrated;
                if (nc is null)
                {
                    newProrated = 0m; // removed component
                }
                else if (oc is not null && oc.FullAmount > 0m)
                {
                    // Apply the same realized factor this component had in the old run.
                    newProrated = Math.Round(nc.FullAmount * oc.ProratedAmount / oc.FullAmount, 2, MidpointRounding.AwayFromZero);
                }
                else
                {
                    // Added component (no old counterpart): use the month's LOP factor.
                    newProrated = Math.Round(nc.FullAmount * monthFactor, 2, MidpointRounding.AwayFromZero);
                }

                decimal oldProrated = oc?.ProratedAmount ?? 0m;
                decimal diff = newProrated - oldProrated;
                if (diff == 0m) continue;

                if (byCode.TryGetValue(code, out ArrearLine? existing))
                {
                    byCode[code] = existing with { Amount = existing.Amount + diff };
                }
                else
                {
                    byCode[code] = nc is null
                        ? new ArrearLine(Guid.Empty, code, code, diff, IsTaxable: true,
                            ConsiderForEpf: false, ConsiderForEsi: false)
                        : new ArrearLine(nc.ComponentId, nc.Code, nc.Name, diff, nc.IsTaxable,
                            ConsiderForEpf: false, ConsiderForEsi: false);
                }
            }
        }

        List<ArrearLine> lines = byCode.Values.Where(l => l.Amount != 0m).ToList();
        decimal total = lines.Sum(l => l.Amount);
        decimal taxable = lines.Where(l => l.IsTaxable).Sum(l => l.Amount);

        // v1: do not recover already-paid net pay. A net-negative settlement clamps to
        // zero — the (lower) structure still applies forward; no arrear is paid.
        if (total <= 0m)
            return new ArrearComputation([], 0m, 0m);

        return new ArrearComputation(lines, total, Math.Max(0m, taxable));
    }

    // payableDays/baseDays as realized in the old run, recovered from any component that
    // was prorated (prorated < full). Flat / no-LOP components have full == prorated and
    // are skipped. Returns 1 when nothing was prorated that month.
    private static decimal DeriveMonthFactor(IReadOnlyList<ArrearOldComponent> old)
    {
        foreach (ArrearOldComponent o in old)
        {
            if (o.FullAmount > 0m && o.ProratedAmount != o.FullAmount)
                return o.ProratedAmount / o.FullAmount;
        }
        return 1m;
    }
}
