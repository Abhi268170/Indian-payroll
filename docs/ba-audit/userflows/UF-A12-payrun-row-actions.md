# UF-A12: Pay Run Per-Employee Row Actions (Overflow Menu)

**Module:** Pay Runs > Pay Run Summary > Employee Summary tab
**Tested:** 2026-05-16
**Pay run:** May 2026 Regular Payroll — PAID (ID: 3848927000000034159)
**Route:** `#/payruns/3848927000000034159/summary`

---

## Findings

### 1. Pay Run Summary Page Structure

**Route:** `#/payruns/{id}/summary`
**Page title:** "Pay Runs | Summary | Zoho Payroll"

**Top header:**
| Field | Value |
|-------|-------|
| Pay run type | Regular Payroll |
| Status badge | Paid |
| Payroll Cost | ₹87,484.00 |
| Total Net Pay | ₹87,484.00 |
| Pay Day | 29 May 2026 |
| Period | 01/05/2026 – 31/05/2026 |
| Base Days | 31 |
| Month | May 2026 |

**Header-level actions:**
| Action | Notes |
|--------|-------|
| Send Payslip | Bulk send payslips to all paid employees |
| Download Bank Advice | Downloads bank statement file (XLS, multiple bank formats) |
| ( 3 Skipped ) | Button to filter/view only skipped employees |

**Three main tabs:**

| Tab | Route suffix | Content |
|-----|-------------|---------|
| Employee Summary | (default) | Per-employee table |
| Taxes & Deductions | `?selectedTab=taxes` | Tax summary (not tested in this session) |
| Overall Insights | `?selectedTab=insights` | Aggregate breakdowns |

**Employee Summary table column headers:**
| Column | Notes |
|--------|-------|
| Employee Name | Clickable button — navigates to employee profile |
| Paid Days | Number of days salary paid |
| Net Pay | ₹ amount |
| Payslip | "View" link — opens payslip slide-in panel |
| TDS Sheet | "View" link — opens TDS computation iframe |
| Payment Mode | Manual Bank Transfer / Direct Deposit / Cheque / Cash |
| Payment Status | "Paid on DD/MM/YYYY" |
| (overflow menu) | ⋮ icon — per-row actions |

**Additional row controls:**
- "All Employees" filter dropdown — filter by paid/skipped/all
- "Export Data" dropdown — export employee data
- "Search Employee" text input — search within current pay run

---

### 2. Per-Row Overflow Menu — PAID Employees

**Trigger:** "payrun-detail-action" dropdown button (aria-label: "Show dropdown menu") in the last column of each paid employee row

**Employee: Arjun Mehta (EMP001)**
| Action | Notes |
|--------|-------|
| Download Payslip | Downloads the employee's payslip PDF directly |
| Send Payslip | Sends payslip PDF to employee's registered email |

**Employee: Priya Sharma (EMP002)**
Same two actions: Download Payslip | Send Payslip

**Business Rule:** Once a pay run is PAID, per-employee row actions are limited to payslip distribution only. No editing, no reversal, no individual employee actions in the row overflow. Those would require a pay run reversal (separate flow).

---

### 3. Per-Row Actions — SKIPPED Employees

**Employees:** Vikram Nair (EMP003), Aisha Khan (EMP004), Rahul Desai (EMP005)

**Skip reason shown inline:** "Skipped — Reason: Onboarding incomplete"

**Overflow menu for skipped employees:** NONE — skipped employee rows have no per-row overflow/action button in a PAID pay run. The row is display-only.

**Business Rule:** Skipped employees in a PAID pay run cannot be added or modified. They would need to be processed in a subsequent pay run or an off-cycle run.

---

### 4. Skipped Employee Row Structure

| Field | Value |
|-------|-------|
| Employee Name | Vikram Nair (EMP003) |
| Status badge | "Skipped" |
| Skip reason | "Reason: Onboarding incomplete" |
| No Paid Days | — |
| No Net Pay | — |
| No Payslip | — |
| No TDS Sheet | — |
| No Payment Mode | — |
| No Payment Status | — |
| No overflow menu | — |

**Skip reasons observed:** "Onboarding incomplete" — this is the only reason seen (EMP003, EMP004, EMP005 all with this reason). Other possible skip reasons (not observed): "Salary Structure not assigned", "LOP full month", "Salary withheld", "Mid-month joiner — no proration".

---

### 5. Employee Row — Click Behaviour

Clicking the employee name button (e.g., "Arjun Mehta (EMP001)") appears to navigate to the employee's profile page (not tested in this session — button style is `icon-button text-start`).

---

### 6. Taxes & Deductions Tab (Not Tested This Session)

Expected content based on Zoho Payroll domain knowledge:
- TDS summary per employee
- PF deductions (employer + employee)
- ESI deductions
- PT deductions
- Total statutory deduction summary

---

## Screenshots / Files

- `payrun-row-overflow-paid.png` — Arjun Mehta row with Download Payslip / Send Payslip menu open

---

## Gaps / Open Questions

- [ ] **Draft/processing pay run overflow menu:** During a pay run that is in Draft/Processing state (not Paid), what additional per-row actions are available? Expected: Edit salary variables, Skip employee, View computation, Unlock salary, etc.
- [ ] **"Send Payslip" confirmation:** When "Send Payslip" is clicked, is there a confirmation modal showing the email address it will be sent to?
- [ ] **"Export Data" options:** What formats and fields does Export Data offer? CSV? Excel? Which fields are included?
- [ ] **Taxes & Deductions tab:** What is the exact structure and data shown?
- [ ] **More skip reasons:** Are there other skip reasons beyond "Onboarding incomplete"? E.g., "Salary withheld manually"?
- [ ] **"( 3 Skipped )" filter:** Clicking this shows only skipped employees. Are there any actions available from this filtered view?
