namespace Payroll.Application.Services;

// WI-018 step 4 — pure arrear math.
//
// Given, for each affected back month, the OLD prorated per-component amounts (read
// from that month's finalised PayrunComponentBreakdown) and the NEW per-component
// full amounts (resolved from the backdated structure via BuildComponentInputs),
// plus that month's persisted proration basis, compute the per-component arrear.
//
// This type is deterministic and I/O-free so the diff rules (full-outer-join across
// changed component sets, proration replay, multi-month accumulation, negative clamp)
// are unit-testable without a database. The orchestration shell that loads runs,
// breakdowns, and the frozen config wraps this.

public sealed record ArrearOldComponent(string Code, decimal ProratedAmount);

public sealed record ArrearNewComponent(
    Guid ComponentId,
    string Code,
    string Name,
    decimal FullAmount,
    bool IsTaxable,
    bool ConsiderForEpf,
    bool ConsiderForEsi,
    bool CalculateOnProRata);

// One affected back month: the proration basis actually applied in that finalised run,
// the old prorated amounts from its breakdown, and the new full amounts for the same month.
public sealed record ArrearMonth(
    int Year,
    int Month,
    int BaseDays,
    int LopDays,
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
    // Mirror of GrossCalculator's per-component proration so recomputed new amounts
    // share the exact denominator the original run used.
    private static decimal Prorate(decimal full, bool calculateOnProRata, int baseDays, int lopDays)
    {
        if (!calculateOnProRata || lopDays <= 0 || baseDays <= 0)
            return full;
        decimal payableDays = baseDays - lopDays;
        return Math.Round(full * payableDays / baseDays, 2, MidpointRounding.AwayFromZero);
    }

    public static ArrearComputation Compute(IReadOnlyList<ArrearMonth> months)
    {
        // Accumulate per-component arrear across all months, keyed by component code
        // (codes are stable across structure versions; component ids may differ).
        var byCode = new Dictionary<string, ArrearLine>(StringComparer.OrdinalIgnoreCase);

        foreach (ArrearMonth m in months)
        {
            var oldByCode = m.Old.ToDictionary(o => o.Code, o => o.ProratedAmount, StringComparer.OrdinalIgnoreCase);
            var newByCode = m.New.ToDictionary(n => n.Code, StringComparer.OrdinalIgnoreCase);

            // Full outer join on code: a component may exist in only the new structure
            // (added → full new amount is arrear) or only the old run (removed → negative).
            var codes = new HashSet<string>(oldByCode.Keys, StringComparer.OrdinalIgnoreCase);
            codes.UnionWith(newByCode.Keys);

            foreach (string code in codes)
            {
                newByCode.TryGetValue(code, out ArrearNewComponent? nc);
                decimal newProrated = nc is null
                    ? 0m
                    : Prorate(nc.FullAmount, nc.CalculateOnProRata, m.BaseDays, m.LopDays);
                decimal oldProrated = oldByCode.TryGetValue(code, out decimal o) ? o : 0m;
                decimal diff = newProrated - oldProrated;
                if (diff == 0m) continue;

                if (byCode.TryGetValue(code, out ArrearLine? existing))
                {
                    byCode[code] = existing with { Amount = existing.Amount + diff };
                }
                else
                {
                    // Identity/flags come from the new component when present; for a
                    // removed component (new side absent) we still surface the recovery
                    // line using the old code, taxable by default (no flags available).
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
}
