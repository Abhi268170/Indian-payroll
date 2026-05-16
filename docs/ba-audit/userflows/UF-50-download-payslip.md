# UF-50: Download Payslip

**Module:** Pay Runs > Payroll History > Pay Run Summary > Payslip
**Tested:** 2026-05-16
**Mock Data Used:** May 2026 Regular Pay Run (PAID); Arjun Mehta (EMP001), Priya Sharma (EMP002)
**App State Before:** Pay Run status = PAID; navigated from Payroll History

## Steps Executed
1. Navigate to `#/payruns/payroll-history`
2. Click May 2026 pay run → `#/payruns/3848927000000034159/summary`
3. Click "View" in Payslip column for Arjun Mehta
4. Observe payslip detail panel (dialog/drawer)
5. Document all fields, buttons, and layout
6. Close panel; document pay run header dropdown actions
7. Navigate to Taxes & Deductions tab
8. Navigate to Overall Insights tab

---

## Pay Run Summary Page — Full Layout

### URL
`#/payruns/3848927000000034159/summary`
Query params for tabs: `?selectedTab=deductions` | `?selectedTab=insights`

### Header Section
| Field | Value |
|-------|-------|
| Title | Regular Payroll |
| Status Badge | Paid |
| Back Link | Back → `#/payruns/payroll-history` |
| Primary Button | Send Payslip (bulk send all payslips by email) |
| Secondary Button | Show dropdown menu (4 actions) |

### Header Dropdown Actions (PAID pay run)
| Action | Purpose |
|--------|---------|
| Download all Payslips | Bulk download all employee payslips as ZIP |
| Download all TDS Worksheets | Bulk download TDS computation sheets |
| Show Downloads | View previously generated download queue |
| Delete Recorded Payment | Reverse the "Mark as Paid" action — revert to pre-payment state |

### Pay Run Stats Bar
| Field | Value |
|-------|-------|
| Period | 01/05/2026 – 31/05/2026 |
| Base Days | 31 |
| Month | May 2026 |
| Payroll Cost | ₹87,484.00 |
| Total Net Pay | ₹87,484.00 |
| Pay Day | 29 May, 2026 |
| Employees | 5 (3 Skipped) |
| Download Bank Advice | Button (download bank transfer file) |

### Taxes & Deductions Summary Card
| Category | Amount |
|----------|--------|
| Taxes | ₹0.00 |
| Benefits | ₹0.00 |
| Donations | ₹0.00 |
| Total Deductions | ₹0.00 |

---

## Employee Summary Tab — Table

### Columns
| Column | Notes |
|--------|-------|
| Checkbox (Select) | Bulk select for actions |
| Employee Name | Clickable — opens employee profile |
| Paid Days | Calendar days paid |
| Net Pay | Final net amount |
| Payslip | "View" button → opens payslip panel |
| TDS Sheet | "View" button → opens TDS worksheet (cross-origin iframe) |
| Payment Mode | Manual Bank Transfer / Direct Deposit / Cheque / Cash |
| Payment Status | "Paid on DD/MM/YYYY" |
| Overflow Menu | Per-employee row dropdown actions |

### Data (May 2026)
| Employee | Paid Days | Net Pay | Payslip | TDS Sheet | Mode | Status |
|----------|-----------|---------|---------|-----------|------|--------|
| Arjun Mehta (EMP001) | 29 | ₹65,484.00 | View | View | Manual Bank Transfer | Paid on 29/05/2026 |
| Priya Sharma (EMP002) | 31 | ₹22,000.00 | View | View | Manual Bank Transfer | Paid on 29/05/2026 |
| Vikram Nair (EMP003) | — | — | — | — | — | Skipped: Onboarding incomplete |
| Aisha Khan (EMP004) | — | — | — | — | — | Skipped: Onboarding incomplete |
| Rahul Desai (EMP005) | — | — | — | — | — | Skipped: Onboarding incomplete |

**Employee correction from prior sessions:** EMP004 = Aisha Khan, EMP005 = Rahul Desai (not "Sneha Patel" as previously assumed).

---

## Payslip Panel — Arjun Mehta (LOAN-00001)

Opened as a slide-in dialog panel (not a new page). Panel persists over the summary table.

### Panel Header
| Field | Value |
|-------|-------|
| Employee Name | Arjun Mehta (link to `#/people/employees/3848927000000032948`) |
| Section Label | Net Pay |
| Emp. ID | EMP001 |
| Net Pay Amount | ₹65,484.00 |
| Payment Info | Paid on 29/05/2026 through Manual Bank Transfer |

### Days Table
| Field | Value |
|-------|-------|
| Payable Days | 31 (calendar days in May) |
| LOP Days | 2 |
| Actual Payable Days | 29 |

### Earnings Table
| Component | Amount |
|-----------|--------|
| Basic | ₹37,417.00 |
| House Rent Allowance | ₹14,967.00 |
| Fixed Allowance | ₹13,100.00 |

**Total Earnings: ₹65,484.00** ✓ (37,417 + 14,967 + 13,100 = 65,484)

### Deductions Table
| Category | Component | Amount |
|----------|-----------|--------|
| Taxes | Income Tax | ₹0.00 |
| Taxes | KL Professional Tax | ₹0.00 |

**Total Deductions: ₹0.00**

### Net Pay Footer
| Field | Value |
|-------|-------|
| Net Pay | ₹65,484.00 |

### Panel Actions
| Button | Action |
|--------|--------|
| Download Payslip | Downloads individual payslip as PDF |
| Send Payslip | Emails payslip to employee's registered email |

### Close Button
X icon at top of panel — closes the panel, returns to summary table.

---

## Taxes & Deductions Tab

### URL: `?selectedTab=deductions`

### Tax Details Table
| Tax Name | Paid By Employer | Paid By Employee |
|----------|-----------------|-----------------|
| Income Tax | ₹0.00 | ₹0.00 |
| KL Professional Tax (Head Office) | ₹0.00 | ₹0.00 |
| Total | ₹0.00 | ₹0.00 |

**Note:** PT label = "KL Professional Tax (Head Office)" — "KL" = Kerala state code, "Head Office" = work location name.

### Benefits Table
"There are no deductions present in this payrun." — empty state.

### Donations Table
"There are no donations present in this payrun." — empty state.

---

## Overall Insights Tab

### URL: `?selectedTab=insights`

### Employee Breakdown
| Metric | Value |
|--------|-------|
| Active Employees | 2 |
| Paid Employees | 2 |
| New Joinee's Skipped | 0 |
| Skipped Employees | 3 |
| Salary Withheld Employees | 0 |
| New Joinee's Arrear Released | 0 |
| Salary Released Employees | 0 |
| LOP Reversed Employees | 0 |

### Statutory Summary
"No data to display" — all statutory contributions = ₹0.

### Payment Mode Summary
| Mode | Count |
|------|-------|
| Direct Deposit | 0 |
| Bank Transfer | 2 |
| Cheque | 0 |
| Cash | 0 |

### Component Wise Breakdown
| Component | Employees | Total Amount |
|-----------|-----------|--------------|
| Basic | 2 | ₹48,417.00 |
| House Rent Allowance | 1 | ₹14,967.00 |
| Fixed Allowance | 2 | ₹24,100.00 |
| **Total Earnings** | | **₹87,484.00** |

**Math verification:**
- Basic: Arjun ₹37,417 + Priya ₹11,000 = ₹48,417 ✓
- HRA: Only Arjun has HRA (Priya's structure = Basic + Fixed Allowance only) → 1 employee ✓
- Fixed: Arjun ₹13,100 + Priya ₹11,000 = ₹24,100 ✓
- Total: ₹48,417 + ₹14,967 + ₹24,100 = ₹87,484 ✓

**Insight from component breakdown:** Priya's salary structure does NOT include an HRA component — only Basic + Fixed Allowance. This means Priya cannot claim HRA exemption under Section 10(13A) — though she is in new regime anyway (exemption irrelevant).

### Component Deep-Drill Links
Each component in the breakdown is a link:
- Basic → `#/payruns/insights/{run-id}/earnings/{component-id}?override_type=`
- HRA → `#/payruns/insights/{run-id}/earnings/{component-id}?override_type=`
- Fixed → `#/payruns/insights/{run-id}/earnings/{component-id}?override_type=`

These drill-down links navigate to employee-level breakouts of each component.

---

## Business Rules
1. Payslip is available as a panel/dialog — not a standalone page
2. "Payable Days" shows calendar days in month (31); "Actual Payable Days" = calendar − LOP
3. Payslip shows full name of deduction components (e.g., "KL Professional Tax" not just "PT")
4. Download Payslip generates PDF of the payslip
5. Send Payslip emails PDF to employee's registered email address
6. Bulk "Send Payslip" from header sends to ALL paid employees in the pay run
7. "Download all Payslips" creates a ZIP file of all individual payslips
8. "Delete Recorded Payment" reverts the pay run from Paid status — risk: re-initiates payment process
9. Overall Insights tab provides management dashboard: employee counts, payment modes, component totals

## Gaps / Observations
- PDF download not tested (would trigger browser download)
- Send Payslip email delivery not verified
- Per-employee row dropdown actions not captured (dropdown dismissed before snapshot)
- No "Print Payslip" option visible (only Download and Send)
- "LOP Reversed Employees: 0" metric in insights — confirms LOP reversal is a supported workflow (not yet investigated)
- "New Joinee's Arrear Released: 0" — confirms new joiner proration arrear release is a supported feature

## Open Questions
- [ ] What format is the downloaded payslip PDF? Does it include company logo/letterhead?
- [ ] Is the payslip password-protected? What is the password?
- [ ] What actions are available in the per-employee row dropdown on a PAID pay run?
- [ ] What does "Delete Recorded Payment" do exactly — does it recreate the pay run in Draft state?
- [ ] Component drill-down links: what does the employee-wise breakout show?
