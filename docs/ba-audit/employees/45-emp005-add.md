# Employees > EMP005 — Standard Employee Addition (Rahul Desai)

## Employee Spec
- **Name:** Rahul Desai
- **Employee ID:** EMP005
- **Designation:** QA Engineer (created inline)
- **Department:** Engineering
- **Work Location:** Head Office (Mumbai HQ)
- **Date of Joining:** 01/06/2024 (earliest DOJ in the test set — FY2024-25)
- **Gender:** Male
- **DOB:** Not specified in spec (not entered)
- **Bank:** Kotak Mahindra Bank, A/C 9876543210011, IFSC KKBK0001234, Savings
- **Gross CTC:** ₹4,80,000/year (₹40,000/month)
- **Zoho Employee ID (internal):** 3848927000000034063

## Key Observations

### Earliest DOJ Across All Test Employees
EMP005's DOJ = 01/06/2024. This is in FY2024-25 (Apr 2024 – Mar 2025). All other test employees have DOJ in FY2025-26. This means:
- For payroll runs initiated in FY2025-26, EMP005 already has a full year of prior employment in this org.
- Any YTD accumulation from FY2024-25 (within this org) should be visible in reports.
- In Zoho, the FY selector in Payslips & Forms tab allows switching back to FY2024-25 to view past payslips if any pay runs were processed.

Since no pay runs have been executed in the audit org, EMP005 has no payslip history despite the early DOJ.

### Designation: "QA Engineer" (New, Created Inline)
- "QA Engineer" not pre-existing — created via inline "New Designation" modal
- Available for future employees after creation
- Pattern consistent with EMP002 (Junior Developer) and EMP004 (UX Consultant)

### Kotak Bank IFSC
IFSC `KKBK0001234` — mock IFSC (Kotak bank prefix KKBK). Zoho lookup failed. Bank Name manually entered as "Kotak Mahindra Bank". 4th consecutive mock IFSC failure — consistent pattern established across all 5 test employees.

## Salary Structure (As Created)
- Annual CTC: ₹4,80,000
- Basic: 50% of CTC = ₹20,000/month (₹2,40,000/year)
- Fixed Allowance: ₹20,000/month residual (₹2,40,000/year)
- No HRA added

### ESI Eligibility Assessment
EMP005 gross salary = ₹40,000/month. ESI ceiling = ₹21,000/month. Not ESI eligible. Even if ESI were configured at org level, EMP005 would not have ESI deducted.

Note: Only an employee earning ≤ ₹21,000/month gross would be ESI eligible in this org. None of the 5 test employees meet this threshold:
- EMP001: ₹70,000 — not eligible
- EMP002: ₹22,000 — not eligible (just above ceiling)
- EMP003: ₹1,50,000 — not eligible
- EMP004: ₹60,000 — not eligible
- EMP005: ₹40,000 — not eligible

To test ESI flows, a separate test employee with CTC ≤ ₹21,000 would need to be created.

## Wizard Flow Summary
EMP005 creation followed the standard 4-step wizard with no deviations beyond what was documented in items 35–38 (Basic Details, Salary Details, Personal Details, Payment Information). No new discoveries from this creation.

### What EMP005 Confirms
- 5th consecutive employee created without issues (after DOJ calendar-click fix was established)
- The wizard flow is consistent and repeatable for standard employees
- Engineering department reuse works correctly (existing department pre-selected)
- DOJ in prior financial year creates no blocking issues

## PT for EMP005
- Work Location = Head Office (Maharashtra) → Maharashtra PT applies
- Monthly gross ₹40,000 → PT slab: ₹7,500–₹10,000 range for Maharashtra = ₹175/month; actually for ₹40,000 it falls in the ₹10,001–₹25,000 = ₹175 bracket or ₹25,001+ = ₹200 bracket. At ₹40,000: Maharashtra PT = ₹200/month (12 months = ₹2,400/year, except February = ₹300 per Maharashtra PT schedule).

## Key Observations for Our Build
1. **DOJ can predate current financial year** — engine must handle employees with DOJ in prior FYs who are now in a new FY. Salary structure effective-from dates must be tracked per FY.
2. **ESI test gap** — none of the 5 test employees cover ESI eligibility. A ₹15,000–₹21,000 CTC employee would be needed for ESI flow testing.
3. **Designation master** — all 5 employees together created 3 new designations (Junior Developer, UX Consultant, QA Engineer) and used 1 pre-existing (Senior Software Engineer, Engineering Manager). Our designation entity is a shared org-level master.
4. **IFSC lookup** — 4 of 5 employees used mock IFSCs that failed lookup. Production IFSC database integration is critical.

## Cross-Module Impact
- EMP005 DOJ = 01/06/2024 means a salary revision test (item 47) can demonstrate revision with effective date mid-FY.
- EMP005 is the candidate for the exit flow (item 49) — simulating employee termination.
- EMP005 in Engineering department → will appear in Engineering-filtered reports.

## Screenshots
- No specific screenshots beyond standard wizard flow.
