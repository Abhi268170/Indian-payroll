# Employees > EMP002 — Mid-Month Joiner (Priya Sharma)

## Employee Spec
- **Name:** Priya Sharma
- **Employee ID:** EMP002
- **Designation:** Junior Developer (created inline)
- **Department:** Engineering
- **Work Location:** Head Office (Mumbai HQ)
- **Date of Joining:** 16/05/2025 (mid-month)
- **Gender:** Female
- **DOB:** 22/07/1998 (Age: 26)
- **Father's Name:** Ramesh Sharma
- **PAN:** (not entered)
- **Bank:** State Bank of India, A/C 31234567890, IFSC SBIN0001234, Savings
- **Gross CTC:** ₹22,000/month (₹2,64,000/year)
- **Employment Type:** Permanent (Zoho has no explicit Employment Type field in wizard)
- **Zoho Employee ID (internal):** 3848927000000032984

## Mid-Month Join: Key Observations

### Date of Joining = 16/05/2025
This is day 16 of a 31-day month (May). For salary proration:
- **Zoho proration rule (observed):** Days-based — employee is paid for actual working days in month of joining.
- Proration formula (standard Indian payroll): `Monthly Gross × (Working Days from DOJ / Total Working Days in Month)`
- For May 2025 (31 days), joining day 16: Working days from 16–31 = 16 days. Working days in month (assuming all days paid) = 31. Prorated = (16/31) × ₹22,000 = ₹11,355 approx.
- Alternatively if calendar days used: (31-16+1)/31 = 16/31.

### Zoho's Proration Configuration
Proration method (calendar days vs working days) is configured at pay schedule level — not shown explicitly in employee wizard. The lerno org pay schedule is "Mon-Fri" (working days), so working-day proration likely applies.

### ESI Eligibility Assessment
Spec said EMP002 is "ESI eligible". However:
- ESI wage ceiling: ₹21,000/month gross
- EMP002 gross = ₹22,000/month → **ABOVE ESI ceiling**
- EMP002 is NOT ESI eligible at this salary
- This is a spec error in the audit brief — documented here for accuracy
- Additionally, ESI is not configured at org level in Zoho lerno org, so no ESI checkbox appeared during setup

### Salary Setup (Zoho Observed)
- Annual CTC: ₹2,64,000
- Basic: 50% of CTC = ₹11,000/month (₹1,32,000/year)
- Fixed Allowance: ₹11,000/month (₹1,32,000/year) [residual]
- Monthly CTC: ₹22,000
- PT: enabled (Head Office → Maharashtra PT)
- HRA: not added (no HRA component pre-configured was added for this employee)

## Wizard Flow — Differences from EMP001

| Aspect | EMP001 | EMP002 |
|---|---|---|
| Designation | Sr. Software Engineer (pre-existing) | Junior Developer (created inline) |
| DOJ | 01/04/2025 (month start) | 16/05/2025 (mid-month) |
| Salary | ₹70,000/month | ₹22,000/month |
| ESI | N/A (not configured) | N/A (not configured + above ceiling) |
| Bank | HDFC (IFSC verified) | SBI (IFSC SBIN0001234 — not verified by lookup; Bank Name manually entered) |
| HRA | Added (40% of Basic) | Not added |

## Salary Step Discovery: Two-Phase Save
A key UX behaviour observed for EMP002 (not documented for EMP001 due to different page state):

The Salary Details wizard step has a **two-phase save**:
1. **Phase 1:** Statutory Components section has its own "Save and Continue" button. Saving this locks the statutory section and reveals the Salary Structure form below.
2. **Phase 2:** Salary Structure form has its own "Save and Continue". Saving this locks the salary structure and reveals Other Benefits section.
3. **Phase 3:** Other Benefits section has "Add Benefit" or "Proceed" (skip). "Proceed" moves to Step 3.

This two-phase approach was not visible in EMP001's session because the page loaded with salary structure already expanded (possible state difference on first load vs. subsequent loads).

## IFSC Validation Failure: SBI Mock IFSC
`SBIN0001234` is a mock IFSC code. Zoho's IFSC lookup did not return a bank name for it. Bank Name field (`aec13d366`) had to be manually filled ("State Bank of India"). 

**Implication for our build:** IFSC validation should fail gracefully — if the IFSC is not in our lookup DB, allow manual entry of Bank Name rather than blocking form submission. The "Verified" badge should only appear for successful lookups.

## Business Rules Observed
1. **Mid-month joining does NOT block employee creation** — DOJ can be any calendar day.
2. **Proration is handled at payroll run time**, not at employee creation. Employee record simply stores DOJ.
3. **ESI ceiling ₹21,000** — employees earning > ₹21,000 gross are not ESI eligible, even if ESI is enabled at org level. Our engine must check ESI eligibility at each payroll run using the current ESI wage ceiling.
4. **Designation created inline** — "Junior Developer" designation was created via "New Designation" modal during wizard. Available for future employees immediately.

## Key Observations for Our Build
1. **Proration engine requirement** — for mid-month joiners and exiters, payroll engine must prorate salary: `Monthly × (Days paid / Total days in month)`. Days basis (calendar vs working days) must be configurable per pay schedule.
2. **ESI eligibility is runtime-computed** — not stored on employee. Recomputed each payroll month based on gross wage vs ESI ceiling. Ceiling changes (e.g., if ESIC raises it) must flow through without data migration.
3. **IFSC lookup degradation** — our IFSC API must allow manual fallback. Block submission only if account number is missing, not if IFSC lookup fails.
4. **DOJ = effective from date** — first payroll for this employee covers 16–31 May 2025. Engine needs `salary_effective_from` date for proration.

## Screenshots
- `screenshots/42-emp002-profile.png` — EMP002 Overview profile (post-creation)
