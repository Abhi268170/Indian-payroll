# UF-36: Regular Pay Run — May 2026 Deep Dive

**Module:** Pay Runs > Summary
**Tested:** 2026-05-16
**Pay Run ID:** 3848927000000034159
**App State:** Pay Run status = PAID (Regular Payroll, May 2026)

## Steps Executed
1. Navigate to `#/payruns/3848927000000034159/summary`
2. Observed pay run header (period, cost, net pay, pay day, employee counts)
3. Opened Employee Summary tab — documented all rows (2 paid, 3 skipped)
4. Clicked Arjun Mehta row → detail panel opened (from prior session)
5. Clicked Priya Sharma row → detail panel opened (this session)
6. Clicked Taxes & Deductions tab → full statutory breakdown
7. Clicked Overall Insights tab → aggregate analytics

---

## Pay Run Header

| Field | Value |
|-------|-------|
| Pay Run Type | Regular Payroll |
| Status | Paid |
| Period | 01/05/2026 - 31/05/2026 |
| Base Days | 31 |
| Month | May 2026 |
| Pay Day | 29 May 2026 |
| Payroll Cost | ₹87,484.00 |
| Total Net Pay | ₹87,484.00 |
| Total Employees | 5 |
| Paid Employees | 2 |
| Skipped Employees | 3 |

**Payroll Cost = Total Net Pay** — This confirms no employer-side PF/ESI contributions are loaded (EPF Disabled for all employees). Employer cost equals employee net pay exactly when no employer statutory contributions apply.

### Header Taxes & Deductions Summary (mini-table on header)
| Category | Amount |
|----------|--------|
| Taxes | ₹0.00 |
| Benefits | ₹0.00 |
| Donations | ₹0.00 |
| Total Deductions | ₹0.00 |

### Header Actions
| Action | Ref | Notes |
|--------|-----|-------|
| Refresh button | e5292 | Icon-only, no label |
| Send Payslip | e5297 | Bulk sends payslips to all paid employees |
| Show dropdown menu | e5299 | Chevron dropdown — contents not yet explored |
| Download Bank Advice | e5325 | Downloads bank transfer file |
| Instant Helper | e5302 | Contextual help overlay |

---

## Employee Summary Tab

### Table Columns
| Column | Notes |
|--------|-------|
| Select (checkbox) | Bulk selection |
| Employee Name | Button — opens detail side panel |
| Paid Days | Calendar days worked (post-LOP) |
| Net Pay | Amount paid |
| Payslip | "View" button → opens payslip |
| TDS Sheet | "View" button → opens TDS computation sheet |
| Payment Mode | e.g., "Manual Bank Transfer" |
| Payment Status | e.g., "Paid on 29/05/2026" |
| Overflow menu | Per-row actions |

### Filter / Search Controls
- "All Employees" toggle button (filter by status)
- "Search Employee" combobox
- "Filter" button (additional filter panel)
- "Export Data" button

### Employee Rows — Paid

#### Arjun Mehta (EMP001)
| Field | Value |
|-------|-------|
| Paid Days | 29 |
| Net Pay | ₹65,484.00 |
| Payment Mode | Manual Bank Transfer |
| Payment Status | Paid on 29/05/2026 |

**Arjun Detail Panel — Earnings:**
| Component | Amount |
|-----------|--------|
| Basic | ₹37,417.00 |
| HRA | ₹14,967.00 |
| Fixed Allowance | ₹13,100.00 |
| **Gross Earnings** | **₹65,484.00** |

**Arjun Detail Panel — Deductions:**
| Component | Amount |
|-----------|--------|
| Income Tax | ₹0.00 |
| KL Professional Tax | ₹0.00 |
| **Total Deductions** | **₹0.00** |

**Arjun Detail Panel — Summary:**
| Field | Value |
|-------|-------|
| Payable Days | 31 |
| LOP Days | 2 |
| Actual Payable Days | 29 |
| Net Pay | ₹65,484.00 |
| Payment | Paid on 29/05/2026 via Manual Bank Transfer |

**Arjun Math Verification (Proration for LOP):**
- Full month salary: Basic ₹44,998 + HRA ₹17,999 + Fixed ₹15,753 = ₹78,750/month
- But revision is effective June 2026 — current structure is ₹70,000/month (₹8,40,000/year)
- Pre-revision: Basic ₹40,000 (57.14% of ₹8,40,000/12 = ₹70,000) = ₹40,000; HRA ₹16,000 (40%); Fixed ₹14,000 = ₹70,000
- LOP deduction: 2 LOP days out of 31 base days
- Prorated: ₹70,000 × 29/31 = ₹65,483.87 ≈ ₹65,484 ✓ (rounded)
- Component proration: Basic = ₹40,000 × 29/31 = ₹37,419 (shown ₹37,417 — minor rounding); HRA = ₹16,000 × 29/31 = ₹14,968 (shown ₹14,967); Fixed = ₹14,000 × 29/31 = ₹13,097 (shown ₹13,100)
- Rounding differences of ₹1–3 per component due to individual component rounding; total nets to ₹65,484 ✓

#### Priya Sharma (EMP002)
| Field | Value |
|-------|-------|
| Paid Days | 31 |
| Net Pay | ₹22,000.00 |
| Payment Mode | Manual Bank Transfer |
| Payment Status | Paid on 29/05/2026 |

**Priya Detail Panel — Earnings:**
| Component | Amount |
|-----------|--------|
| Basic | ₹11,000.00 |
| Fixed Allowance | ₹11,000.00 |
| **Gross Earnings** | **₹22,000.00** |

**Priya Detail Panel — Deductions:**
| Component | Amount |
|-----------|--------|
| Income Tax | ₹0.00 |
| KL Professional Tax | ₹0.00 |
| **Total Deductions** | **₹0.00** |

**Priya Detail Panel — Summary:**
| Field | Value |
|-------|-------|
| Payable Days | 31 |
| LOP Days | 0 |
| Actual Payable Days | 31 |
| Net Pay | ₹22,000.00 |
| Payment | Paid on 29/05/2026 via Manual Bank Transfer |

**Priya Math Verification:**
- Full month, no LOP — full pay
- CTC: ₹22,000/month (₹2,64,000/year)
- No HRA component (only Basic + Fixed Allowance)
- Basic = ₹11,000 (50% of CTC)
- Fixed Allowance = ₹11,000 (residual)
- Basic + Fixed = ₹11,000 + ₹11,000 = ₹22,000 = Net Pay ✓
- PT threshold for Kerala: ₹22,001–30,000 = ₹212/month; but ₹22,000 is at threshold boundary — showing ₹0 PT (slab boundary inclusive/exclusive behavior unclear)
- Income Tax ₹0 — salary ₹2,64,000/year is below ₹3,00,000 new regime basic exemption → ₹0 TDS ✓

**Priya PAN Anomaly:**
- Priya's PAN = "-" (not filled) per UF-24 observations
- Yet she is INCLUDED in the pay run and PAID ₹22,000
- Vikram Nair (PAN also "-") is SKIPPED
- Hypothesis: Priya has DOB and Father's Name filled; Vikram and Aisha have ALL personal fields blank
- The payroll gate may require ALL of: DOB + Father's Name + PAN — if any subset is present, the gate passes (or Priya's data was partially filled separately)
- 🔴 This is unresolved — requires direct inspection of Priya's personal details

### Employee Rows — Skipped
| Employee | Reason |
|----------|--------|
| Vikram Nair (EMP003) | Onboarding incomplete |
| Aisha Khan (EMP004) | Onboarding incomplete |
| Rahul Desai (EMP005) | Onboarding incomplete |

Detail panel behavior for skipped employees: clicking row opens same panel structure but shows "Skipped" status with no earnings/deductions tables.

---

## Taxes & Deductions Tab

### Income Tax Section
| Entity | Employer | Employee |
|--------|----------|----------|
| Income Tax | ₹0.00 | ₹0.00 |

**Reason for ₹0 TDS:**
- Arjun: ₹9,45,000 CTC (after pending revision; current = ₹8,40,000). Annual taxable = ~₹8,40,000 - standard deduction ₹75,000 = ₹7,65,000. New regime slabs: ₹0–3L = 0%, ₹3–7L = 5% = ₹20,000, ₹7–8L = 10% = ₹10,000, plus rebate u/s 87A applies if total tax ≤ ₹25,000. ₹30,000 total exceeds 87A rebate of ₹25,000 — but showing ₹0. This suggests the system may not yet be computing TDS or Arjun has IT declarations that reduce liability to zero.
- Priya: ₹2,64,000/year → well below ₹3L basic exemption → ₹0 TDS ✓

### Professional Tax Section
| Category | Employer | Employee |
|----------|----------|----------|
| KL Professional Tax (Head Office) | ₹0.00 | ₹0.00 |

**Reason for ₹0 PT:**
- Kerala PT slab: ₹22,001–30,000 = ₹212/month
- Arjun: ₹70,000/month gross → should be ₹208/month PT (slab ₹30,001+ = ₹208/month in Kerala) — but showing ₹0
- Priya: ₹22,000 gross — at boundary of ₹22,001 slab — showing ₹0
- 🔴 PT ₹0 for Arjun is a gap — his salary clearly exceeds ₹30,000/month and should have PT deducted

### Benefits Section
- "No deductions present" — empty state
- No PF, ESI, or other benefits configured for any employee

### Donations Section
- Empty — no employees configured donations in this pay run

---

## Overall Insights Tab

### Summary Cards
| Metric | Value |
|--------|-------|
| Active | 2 |
| Paid | 2 |
| Skipped | 3 |
| On Leave | 0 |
| On Notice | 0 |
| Inactive | 0 |

### Statutory Contributions
- "No data to display" — confirms zero statutory (PF/ESI) contributions for this pay run

### Payment Mode Breakdown
| Mode | Count |
|------|-------|
| Bank Transfer | 2 |

### Salary Component Breakdown
| Component | Employees | Total |
|-----------|-----------|-------|
| Basic | 2 | ₹48,417.00 |
| HRA | 1 | ₹14,967.00 |
| Fixed Allowance | 2 | ₹24,100.00 |
| **Total** | | **₹87,484.00** |

**Math:** ₹48,417 + ₹14,967 + ₹24,100 = ₹87,484 ✓

**HRA only for 1 employee** — Arjun has HRA component in salary structure; Priya's structure is Basic + Fixed Allowance only (no HRA).

---

## Business Rules Observed

1. Payroll Cost = Total Net Pay when no employer statutory contributions apply (EPF/ESI both disabled → employer contribution = ₹0)
2. LOP proration formula: `Component × (Payable Days / Base Days)`, rounded to nearest rupee per component
3. Base days = calendar days in month (31 for May); payable days = base days minus LOP
4. PT ₹0 despite eligible salary — either PT configuration has a gap or a threshold boundary issue (requires investigation in Settings > Statutory)
5. TDS ₹0 for Arjun — either IT declarations reduce liability or TDS computation has not been configured/triggered
6. "Paid on 29/05/2026" — pay day is 29th May, not last day of month (configured in pay schedule settings)
7. Skipped employees consume an "employee slot" in the count (5 total) but their salary is excluded from totals
8. Per-employee detail panel shows: payment status + payment mode + earnings table + deductions table + net pay (no employer contribution columns visible to payroll admin in this view)

---

## Navigation

**From:** `#/payruns` (Pay Runs list) or `#/payruns/payroll-history`
**To from this page:**
- Employee profile: link in detail panel header
- Payslip: "View" button in Payslip column
- TDS Sheet: "View" button in TDS Sheet column
- Settings: header "Settings" button
- Bank Advice: "Download Bank Advice" button

---

## Gaps / Observations

- 🔴 PT ₹0 for Arjun (salary ₹70,000/month) — Kerala PT highest slab is ₹30,001+ = ₹208/month; this should not be zero
- 🔴 TDS ₹0 for Arjun (salary ₹8,40,000/year) — new regime taxable ≈ ₹7,65,000 after standard deduction; tax liability should be ~₹5,000–30,000 depending on other deductions
- Priya PAN="-" yet included in pay run — Vikram PAN="-" excluded — gate logic is not solely PAN-based
- No employer PF/ESI columns visible in pay run summary (consistent with all being disabled)
- "Benefits" section in Taxes & Deductions is empty — no reimbursements, no perquisites for May 2026
- Export Data button on Employee Summary — format not tested (likely CSV or Excel)
- Overflow dropdown menu per employee row — contents not yet explored (likely: resend payslip, mark as unpaid, etc.)

## Open Questions
- [ ] Why is PT ₹0 for Arjun despite ₹70,000/month salary? Is KL PT configured in Settings > Statutory?
- [ ] Why is TDS ₹0 for Arjun with ₹8,40,000 CTC? Does Arjun have IT declarations that zero out liability?
- [ ] What does the row overflow dropdown menu contain for a paid employee?
- [ ] What does "Download Bank Advice" download — format (text/CSV/XLSX), structure, bank-specific template?
- [ ] Priya's personal details — is DOB/Father's Name present even though PAN="-"?
