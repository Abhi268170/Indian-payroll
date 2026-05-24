# UF-49: Skipped Employees in Pay Run

**Module:** Pay Runs > Overall Insights Tab > Skipped Section
**Tested:** 2026-05-16
**Mock Data Used:** May 2026 pay run; Skipped = 3 (Vikram Nair, Aisha Khan, Rahul Desai)
**App State Before:** May 2026 pay run PAID

---

## Skipped Employees — Confirmed State

From UF-50 (Overall Insights tab of May 2026 pay run):
| Metric | Value |
|--------|-------|
| Active | 2 |
| Paid | 2 |
| Skipped | 3 |
| Bank Transfer | 2 |

**Skipped employees:** Vikram Nair (EMP003), Aisha Khan (EMP004), Rahul Desai (EMP005)

---

## Why Employees Are Skipped

Zoho Payroll applies an **onboarding completeness gate** before including an employee in a pay run.

### Onboarding Completeness Check (Composite)
An employee is skipped if ANY of the following are missing:
- Date of Birth
- Father's Name / Husband's Name
- Personal Email
- Permanent Address
- Bank Account Details
- PAN (for TDS computation)
- Salary Structure Assignment

**Not a PAN-only gate** — confirmed by Priya's inclusion despite no PAN (but having other required fields).

### Vikram Nair (EMP003) — Root Cause of Skipping
Missing fields (from prior session observations):
- Date of Birth
- Father's Name
- Personal Email
- Permanent Address
(Nearly all personal details absent — emergency hire onboarding not completed)

---

## Pay Run UI — Skipped Section (Expected)

In the pay run summary, skipped employees appear in a separate section:

| Employee | Status | Reason for Skipping |
|----------|--------|---------------------|
| Vikram Nair | Skipped | Onboarding incomplete |
| Aisha Khan | Skipped | Onboarding incomplete |
| Rahul Desai | Skipped | Onboarding incomplete |

Possible UI pattern:
- "Skipped (3)" badge/tab in employee list
- Tooltip or "Why skipped?" link showing specific missing fields
- "Complete Onboarding" quick action linking to employee profile

---

## Completing Onboarding to Include in Pay Run

### Steps to Unblock Vikram Nair
1. Navigate to Employees > Vikram Nair
2. Complete all required fields:
   - Personal tab: DOB, Father's Name, Personal Email
   - Address tab: Permanent Address
   - Bank tab: Account number, IFSC
3. Save
4. Create an off-cycle pay run OR wait for next month's regular run

**Note:** Vikram cannot be added to the PAID May 2026 pay run — it is finalized. He would need to be paid via:
1. Off-cycle pay run for May (manually created)
2. June pay run (with arrears for May if applicable)

---

## Skipped Employees and Loans

**Financial risk confirmed in UF-64:**
- Vikram Nair has LOAN-00002 (₹1,00,000 Emergency Loan)
- While skipped: No EMI deductions
- Outstanding balance remains ₹1,00,000 until onboarding complete
- Employer has disbursed ₹1,00,000 but cannot recover via payroll

---

## Skipped Employees and Statutory Compliance

### PF
- Skipped employees: No PF deduction, no employer PF contribution
- EPF ECR: Employee appears with NCP Days = 31 (full month non-contributing)
- If EPFO registration covers these employees: Compliance risk for uncovered months

### ESI
- If ESI-eligible and skipped: No ESI deduction
- ESIC contribution gaps could be flagged in ESI return

### PT
- No PT deduction for skipped month
- PT due in September/March — if employee onboarded before then: PT deducted from first included pay run

### TDS
- No TDS for months when skipped
- Once included: TDS catches up for remaining months of FY
- If employee was never onboarded: No Form 16 (no TDS deducted)

---

## Moving Skipped Employee to Off-Cycle Run

If admin wants to pay a skipped employee for the current month:
1. Complete employee onboarding (all required fields)
2. Create Off-Cycle Pay Run (UF-52)
3. Add the employee to the off-cycle run
4. Process and pay

This creates a separate pay run for the same month — payslip date may differ from regular pay date.

---

## Business Rules
1. Skipped = onboarding incomplete (composite check, not PAN-only)
2. Skipped employees get no payslip, no salary, no deductions for that month
3. Loans on skipped employees continue to accrue (no EMI deduction — outstanding remains)
4. Skipped employees cannot be added to already-finalized pay runs
5. Off-cycle run is the mechanism to pay skipped employees retroactively

## Gaps / Observations
- Exact "skipped" reason per employee not seen in UI (need to check tooltip/link)
- "Why skipped?" indicator UI not confirmed
- Moving from Skipped to Active requires field completion — tested conceptually but not navigated

## Open Questions
- [ ] Does the system show which specific fields are missing for a skipped employee in the pay run UI?
- [ ] Can admin force-include a skipped employee (override the onboarding gate)?
- [ ] If employee is skipped for 3 months and then onboarded, can arrears for those 3 months be processed?
- [ ] Is there a notification sent to the employee when they are skipped from a pay run?
