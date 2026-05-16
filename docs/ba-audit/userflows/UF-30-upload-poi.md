# UF-30: Upload Proof of Investment (POI)

**Module:** Employee Portal / Approvals > Proof of Investments
**Tested:** 2026-05-16
**Mock Data Used:** IT Declaration is LOCKED org-wide
**App State Before:** IT Declaration not released; no POI submissions in system

## Steps Executed
1. Observed IT Declaration settings (`#/settings/preferences/it-declaration`)
2. Identified that IT Declaration is LOCKED — POI upload unavailable to employees
3. Documented expected flow from UI patterns and domain knowledge

---

## Pre-conditions for POI Upload

1. **Admin must release IT Declaration** via `#/settings/preferences/it-declaration` → "Release IT Declaration"
2. Employee must have a Zoho account and portal access enabled
3. Employee must have submitted IT Declaration for the financial year (declaring investment amounts)
4. Admin must have opened the POI submission window (typically Jan-March each year)

---

## POI Upload Flow — Employee Perspective (Expected)

### Via Employee Portal (Mobile App or Web)
1. Employee logs into Zoho Payroll Employee Portal
2. Navigates to "IT Declaration" or "Investments" section
3. Selects the investment category (e.g., 80C — LIC Policy, PPF, ELSS)
4. Views their declared amount
5. Clicks "Upload Proof" for each investment item
6. Selects file (PDF of insurance receipt, passbook scan, etc.)
7. Submits — proof goes to admin for verification

### Via Admin (On Behalf of Employee)
1. Admin navigates to `#/people/employees/{id}/investments-and-proofs`
2. Selects the POI section
3. Uploads document on behalf of employee
4. Marks as submitted

---

## POI Categories (Standard under IT Rules)

| Category | Investment Types | Document Required |
|----------|-----------------|-------------------|
| 80C | LIC premium | Policy receipt/premium certificate |
| 80C | PPF contribution | Passbook or e-statement |
| 80C | ELSS Mutual Fund | SIP statement |
| 80C | NSC | Certificate copy |
| 80C | Home Loan (Principal) | Statement from bank |
| 80D | Health Insurance | Premium receipt |
| 80G | Donations | Receipt with 80G registration |
| HRA | Rent (old regime only) | Rent receipts; landlord PAN if rent > ₹1L/year |
| LTA | Travel | Travel tickets/boarding passes |
| 80CCD(1B) | NPS self-contribution | NPS statement |

**New Regime Note:** Most of the above deductions are NOT applicable under new tax regime (Section 115BAC). Only NPS employer contribution (80CCD(2)) and standard deduction (₹75,000) apply in new regime. POI is primarily relevant for employees on old regime.

---

## Admin Review of POI

After employee uploads:
1. Admin navigates to `#/approvals/proof-of-investment`
2. Sees list of pending POI submissions
3. Opens each document (PDF viewer in browser)
4. Verifies: document type matches investment declared, amount matches, financial year matches
5. Clicks "Approve" or "Reject" with reason

---

## Current System State

- IT Declaration: LOCKED
- Employees cannot submit declarations or POI via portal
- Admin can still submit on behalf of employee via employee profile
- No POI submissions exist in demo org

---

## Business Rules
1. POI submission window is typically open Jan-March for the current FY
2. Once POI is approved, TDS for remaining months adjusts to reflect verified exemptions
3. Rejected POI: investment is treated as declared-only (not proof-verified) — TDS based on declared amount or conservative estimate
4. Employees on new regime: POI is minimal (only NPS, possibly donations)
5. If employee does not submit POI within window, employer defaults to taxing the entire declared amount as non-verified

## Gaps / Observations
- POI upload flow not tested — IT Declaration locked
- No POI document viewer tested
- 🟡 Mark for future session: release IT Declaration, have employee submit declaration + POI
- Approvals > Proof of Investments page not navigated

## Open Questions
- [ ] What file formats are accepted for POI (PDF only, or also JPG/PNG)?
- [ ] Is there a size limit per POI document?
- [ ] Can multiple documents be attached to a single investment category?
- [ ] Is there a system-generated reminder to employees approaching the POI deadline?
