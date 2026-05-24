# Edge Case > Tax Regime Switch (New vs Old Regime per Employee)

## Scenario
EMP001 Arjun Mehta is on New Regime. Test switching to Old Regime, document all UI changes, and verify mid-year regime switching behaviour.

## Steps to Reproduce
1. Navigate to Employees > EMP001 > Investments tab (`#/people/employees/3848927000000032948/investments-and-proofs`)
2. Click "Submit Declaration" link
3. Observe: URL uses `?tax_regime=with_exemptions` — indicating Old Regime is the parameter for the declaration form
4. To select regime: the IT Declaration form collects declarations first, then "Submit and Compare" leads to regime selection on the next screen

## Expected Behaviour (statutory rule)
Per Income Tax Act, an employee may choose between:
- **New Regime (Without Exemptions)**: Default from FY2024-25 onwards; lower slab rates; no deductions (80C, HRA, etc.)
- **Old Regime (With Exemptions)**: Employee opts-in; higher slab rates; all deductions available

Mid-year regime change is NOT allowed under law — the declaration at start of year is final for TDS computation. However, the employee may switch regime at the time of filing their personal return.

## Actual Zoho Behaviour

### IT Declaration Lock State
When navigating to the Investments tab for EMP001:
- **Status**: IT Declaration is locked for this employee
- **Message**: "IT Declaration submission is locked for this employee. You can either allow the employee to submit IT Declaration through the portal or submit it on their behalf."
- **Actions available**: "Submit Declaration" link (admin can submit on behalf)

### Regime Selection Architecture
From the API response (`/api/v1/investmentdeclarations/editpage?employee_id=3848927000000032948`):
```json
{
  "tax_regime": "with_exemptions",
  "tax_regime_formatted": "With Exemptions",
  "is_multiple_tax_regimes_applicable": true,
  "can_change_tax_regime": true,
  "is_employee_rehired_on_same_taxyear": false
}
```

- `is_multiple_tax_regimes_applicable: true` — system supports both regimes
- `can_change_tax_regime: true` — admin CAN change regime (no mid-year lock at UI level)
- The URL parameter `tax_regime=with_exemptions` pre-selects Old Regime when admin clicks "Submit Declaration"
- The form ends with "Submit and Compare" button — implying Zoho shows a side-by-side TDS comparison between regimes before final submission

### POI Report Baseline
From the Proof of Investment Report, all 5 employees show "New Regime" as their current tax regime. EMP001 confirmed as New Regime.

### Declaration Form URL Pattern
- Old Regime (With Exemptions): `#/people/employees/{id}/investment-declaration/new?tax_regime=with_exemptions`
- New Regime: `#/people/employees/{id}/investment-declaration/new?tax_regime=without_exemptions` (inferred)

## Screenshots
- `screenshots/105-investments-locked-state.png` — IT Declaration locked state for EMP001
- `screenshots/106-it-declaration-form-full.png` — IT Declaration form (Old Regime) with all sections

## Gap / Bug / Surprise
1. **No explicit tax regime selector visible on UI** — the admin cannot see a clear "Switch to New/Old Regime" control on the Investments tab. The regime is selected implicitly when clicking "Submit Declaration" (which defaults to Old Regime via URL param). The actual regime selection appears to happen on the "Submit and Compare" next screen, which could not be accessed due to SPA routing interference.
2. **`can_change_tax_regime: true` year-round** — Zoho does NOT enforce the statutory constraint that the regime should be fixed at the start of the year for employer TDS. Employees appear to be able to change throughout the year. This may be intentional (Zoho re-computes TDS from April onwards when regime changes).
3. **IT Declaration locked state** — The employee cannot self-submit because portal access is disabled for EMP001. Admin must submit on behalf.
4. **"Submit and Compare" flow** — Zoho appears to compute TDS under both regimes and shows a comparison, which is best-practice UX but not required by statute.

## How We Should Build This
- Maintain `tax_regime` as a per-employee field on the `EmployeeTaxDeclaration` entity with values `new_regime` | `old_regime`
- Default to `new_regime` (as per Finance Bill 2023 default)
- Allow employer to change regime at the start of fiscal year (April); warn if changed mid-year
- Mid-year change should trigger a TDS recalculation from April of the current FY
- Show a clear regime selection toggle on the IT Declaration form (not hidden in URL params)
- Side-by-side TDS comparison when employee toggles regime is excellent UX — implement this
- Lock regime change after the first payrun of the fiscal year, with admin override requiring explicit confirmation and audit log
