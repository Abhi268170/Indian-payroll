# UF-41: Vikram Nair Proration — Skipped Employee Analysis

**Module:** Pay Runs > Employee Summary (Skipped row) / Employees > Vikram Nair
**Tested:** 2026-05-16
**Mock Data Used:** Vikram Nair (EMP003, ID: 3848927000000034014), May 2026 Pay Run
**App State Before:** Pay Run status = PAID; Vikram shows "Skipped — Reason: Onboarding incomplete"

## Steps Executed
1. Navigate to `#/payruns/3848927000000034159/summary`
2. Observe Vikram Nair row: Skipped, Reason: Onboarding incomplete
3. Navigate to `#/people/employees/3848927000000034014` (Vikram's profile)
4. Observe onboarding completion status and missing fields
5. Cross-reference with Priya Sharma who was included despite PAN="-"

## Vikram Nair — Pay Run Status

In May 2026 pay run Employee Summary table:
| Field | Value |
|-------|-------|
| Employee | Vikram Nair (EMP003) |
| Status | Skipped |
| Reason | Onboarding incomplete |
| Paid Days | — (no value) |
| Net Pay | — (no value) |
| Payslip | — (no button) |
| TDS Sheet | — (no button) |
| Payment Mode | — |
| Payment Status | — |

Clicking on Vikram's row: No detail panel opened (or if it does, it shows the skipped state without earnings/deduction tables — not tested in this session).

## Vikram Nair Profile — Missing Fields (from UF-18)

| Field | Value | Impact |
|-------|-------|--------|
| Employee ID | EMP003 | Present ✓ |
| DOJ | 01/01/2025 | Present ✓ |
| Department | Engineering | Present ✓ |
| Designation | Software Engineer | Present ✓ |
| DOB | "-" | Missing |
| Father's Name | "-" | Missing |
| PAN | "-" | Missing |
| Personal Email | "-" | Missing |
| Address | "-" | Missing |
| Work Email | Present ✓ | Present ✓ |

Salary Structure: ASSIGNED (₹1,50,000/month — Basic ₹75,000 50% + Fixed Allowance ₹75,000)
Statutory: EPF Disabled, ESI Disabled, PT Enabled, LWF Disabled

## Proria vs Vikram — Onboarding Gate Analysis

| Field | Priya Sharma (Included) | Vikram Nair (Skipped) |
|-------|------------------------|----------------------|
| DOB | Present | Missing |
| Father's Name | Present | Missing |
| PAN | "-" (missing!) | "-" (missing!) |
| Personal Email | Present (inferred) | Missing |
| Address | Present (inferred) | Missing |
| Salary Structure | Assigned | Assigned |

Both have PAN="-", but Priya is included and Vikram is skipped. This confirms the onboarding gate is NOT solely PAN-dependent.

**Hypothesis:** The gate checks a composite completeness score. Priya has more personal fields filled (DOB + Father's Name + email + address) even without PAN. Vikram has almost NO personal fields — only work email and DOJ. The gate triggers "Onboarding incomplete" when too many mandatory fields are blank, not just PAN.

**Alternative hypothesis:** Priya's onboarding was completed via an older workflow or imported — the data completeness state persists from the time she was added. Vikram was added more recently with fewer details.

## Proration Impact of Skipping

When Vikram is skipped:
- Zero salary paid to Vikram in May 2026
- No payslip generated
- No TDS, PT, or any deduction computed
- No ECR line for Vikram
- Employer payroll cost does not include Vikram

Vikram's DOJ is 01/01/2025 — if his onboarding had been complete, he would have received a FULL MONTH salary for May (no LOP), since he joined January 2025 (no mid-month join in May).

**Expected salary if included:**
- Monthly CTC: ₹1,50,000
- Basic: ₹75,000, Fixed Allowance: ₹75,000
- No LOP → Paid Days = 31 → Net Pay = ₹1,50,000 (assuming no deductions — EPF disabled, ESI disabled, PT would be ₹0 in May due to half-yearly cycle, TDS depends on IT declaration)

## Proration Formula (If Mid-Month Join Applied)

Since Vikram joined 01/01/2025 (full month — no proration needed in May), the proration formula would only matter if he were a new joiner in May itself.

Zoho Payroll proration formula (observed from Arjun's LOP proration):
`Monthly CTC × (Payable Days / Calendar Days in Month)`

For a mid-May joiner (e.g., joined 15th May):
- Payable Days = 31 − 14 = 17
- Proration = ₹1,50,000 × 17/31 = ₹82,258

The UI does NOT show proration previews before processing — no "preview" button on the employee profile for mid-month joiners.

## How to Fix Vikram's Onboarding

To include Vikram in the next (June) pay run:
1. Navigate to `#/people/employees/3848927000000034014/edit/personal-details`
2. Fill: DOB, Father's Name, Personal Email, Address
3. Optionally add PAN (but this may not be the blocking gate based on Priya's inclusion)
4. Save — no approval needed for personal details edit
5. June pay run will automatically include Vikram when initiated

## UI Behavior for Skipped Employees

- Skipped rows show the employee name as a button (navigable to profile)
- "Reason: Onboarding incomplete" appears in the third cell where "Paid Days" would be
- No checkbox for skipped employees (cannot bulk-select)
- No "Include" override button — cannot force-include a skipped employee
- No link to "complete onboarding" directly from the skipped row

## Gaps / Observations
- 🔴 No force-include override for admins — if a payroll admin is aware that Vikram's missing data is not legally required for salary payment, they cannot bypass the gate
- 🔴 The onboarding gate criteria are opaque — there is no list of "complete these N fields to be included in payroll"
- PAN missing for Priya (included) — suggests PAN is not enforced as a hard gate; however, TDS without PAN defaults to 20% deduction rate per Income Tax rules — yet TDS is ₹0 for Priya (low salary). Risk: if Priya's salary ever crosses taxable threshold, TDS without PAN would be 20% flat rate instead of slab rate.
- "Onboarding incomplete" wording in the pay run does not tell the admin WHICH fields are missing — no actionable detail
- No email/notification to admin about skipped employees when pay run is created — must manually check the skipped count in the header
