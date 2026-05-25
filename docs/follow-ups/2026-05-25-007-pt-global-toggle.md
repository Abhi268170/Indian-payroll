# Follow-up: PT global on/off toggle (audit finding #7)

**Status:** open
**Type:** product + tech alignment
**Originating audit pass:** 2026-05-25 statutory hardening (master @ `9e7aabd`)
**Owner:** _unassigned_

## Background

`StatutoryConfigBuilder.cs:65` hardcodes `PTEnabled: true` when building the
engine's `StatutoryConfig` from the persisted `StatutoryOrgConfig` domain
entity. There is no per-tenant or per-org switch that disables Professional
Tax globally; the only way to opt an employee out today is by working in a
state that has no PT slabs configured.

```csharp
// src/Payroll.Application/Services/StatutoryConfigBuilder.cs:65
PTEnabled: true,
```

The other two engine toggles (`PFEnabled`, `ESIEnabled`) are wired through to
`StatutoryOrgConfig.EpfEnabled` and `StatutoryOrgConfig.EsiEnabled` — so the
inconsistency is visible in the same builder method.

## Why this was deferred from the 2026-05-25 hardening pass

Implementing a real toggle requires:
- Domain field on `StatutoryOrgConfig` (`PtEnabled bool`)
- EF migration
- `ConfigurePtCommand` (or extend the existing PT-config command) + validator
- `StatutoryConfigBuilder` wiring
- Frontend toggle on the Statutory Components → PT tab
- Tests at all layers

That is feature-scope work, not a bug fix. The hardening pass was scoped to
correctness fixes. We deferred this until product confirms the requirement.

## Decisions needed before scheduling

1. **Is there a documented tenant need to disable PT globally?**
   - PT is a state statutory deduction. An org operating in a PT-applicable
     state cannot legally opt out. A global off switch would only be
     meaningful for orgs operating entirely in non-PT states (e.g. UP, Delhi,
     Haryana, Rajasthan, J&K).
   - Confirm: do we have any tenant in this position today or in the pipeline?

2. **Should "no PT slabs configured for any of my work locations" be enough?**
   - The engine already returns `IsExempt = true` from `PTCalculator` when no
     slab matches the employee's `WorkStateCode`. Operators in non-PT states
     get zero PT today without any toggle.
   - This may make the global toggle redundant.

3. **If we do add the toggle, what is the intended UX?**
   - Hidden by default, surfaced only when admin disables PT globally?
   - Banner on PT tab explaining the trade-off?
   - Per-state override vs single org-level switch?

4. **Calculation snapshot implications?**
   - `PayrollRun.StatutoryConfigSnapshot` captures the config at run time.
     Toggling PT mid-FY would not retroactively recompute approved runs (by
     design). Confirm this is the desired behavior.

## Recommendation

Block this until at least one tenant or prospect explicitly asks for it.
The current behavior (auto-exempt when no slab matches the work state)
already covers the realistic case. Adding a toggle that ~no one uses adds
configuration surface area without payoff.

If product greenlights, the implementation plan is small (~half a day) and
the test surface is well-scoped — see `tests/Payroll.Engine.Tests/PTCalculatorTests.cs`
for the existing matrix to extend.

## Linked references

- Audit pass commit: `33d0b1d`
- Builder location: `src/Payroll.Application/Services/StatutoryConfigBuilder.cs:65`
- PT engine surface: `src/Payroll.Engine/Calculators/PTCalculator.cs`
- PT direct tests: `tests/Payroll.Engine.Tests/PTCalculatorTests.cs`
