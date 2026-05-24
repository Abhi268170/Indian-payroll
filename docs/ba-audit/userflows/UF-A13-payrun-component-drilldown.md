# UF-A13: Pay Run Component Drill-Down — Overall Insights Tab

**Module:** Pay Runs > Pay Run Summary > Overall Insights tab
**Tested:** 2026-05-16
**Pay run:** May 2026 Regular Payroll — PAID (ID: 3848927000000034159)
**Route:** `#/payruns/3848927000000034159/summary?selectedTab=insights`

---

## Findings

### 1. Overall Insights Tab Structure

**Route:** `#/payruns/{id}/summary?selectedTab=insights`
**Tab switching:** Clicking "Overall Insights" button updates URL with `?selectedTab=insights`

**Page sections (top to bottom):**

---

### 2. Section A: Employee Breakdown

**Heading:** "Employee Breakdown"

| Category | Count (May 2026) |
|----------|-----------------|
| Active Employees | (total active) |
| Paid Employees | 2 (Arjun, Priya) |
| New Joinees Skipped | 0 |
| Skipped Employees | 3 (Vikram, Aisha, Rahul) |
| Salary Withheld Employees | 0 |
| New Joinees Arrear Released | 0 |
| Salary Released Employees | 0 |
| LOP Reversed Employees | 0 |

**Purpose:** Quick HR analytics on who was paid vs skipped vs withheld. Useful for compliance audits.

---

### 3. Section B: Statutory Summary

**Heading:** "Statutory Summary"
**Content:** "No data to display"

**Expected in production:** Would show:
- PF contributions (employee + employer)
- ESI contributions
- PT deductions
- LWF deductions
- Total statutory burden per component

In this test org: PF/ESI not fully configured for paid employees, hence no statutory data.

---

### 4. Section C: Payment Mode Summary

**Heading:** "Payment Mode Summary"

| Mode | Count | Amount |
|------|-------|--------|
| Direct Deposit Payment Mode | 0 | — |
| Bank Transfer Payment Mode | 2 | ₹87,484.00 |
| Cheque Payment Mode | 0 | — |
| Cash Payment Mode | 0 | — |

Both paid employees (Arjun, Priya) use "Manual Bank Transfer" mode.

---

### 5. Section D: Component Wise Breakdown

**Heading:** "Component Wise Breakdown"
**Table columns:** Components | Employees Involved | Total Amount

**Data observed:**

| Section | Component | Employees Involved | Total Amount |
|---------|-----------|-------------------|--------------|
| Base Earning | — | — | ₹87,484.00 |
| — | Basic | 2 | ₹48,417.00 |
| — | House Rent Allowance | 2 | ₹14,967.00 |
| — | Fixed Allowance | 2 | ₹24,100.00 |
| Total Earnings | — | — | ₹87,484.00 |

**Observations:**
1. Three earning components for 2 employees: Basic, HRA, Fixed Allowance
2. Basic: ₹48,417 = ₹40,000 (Arjun) + ₹8,417 (Priya's prorated or partial)
   - Note: Priya's salary ₹22,000 net / month. If Basic = 40% of gross, it would be ~₹8,800. Exact proration not confirmed.
3. HRA: ₹14,967 total across both employees
4. Fixed Allowance: ₹24,100 total
5. Total = Basic + HRA + Fixed Allowance = ₹48,417 + ₹14,967 + ₹24,100 = ₹87,484 ✓

**Component drill-down (clicking a component):** Not tested. Expected: clicking "Basic" would open a drill-down showing each employee's Basic pay amount, paid days, proration factor.

---

### 6. Component Totals Cross-Reference

From the Employee Summary tab:
- Arjun Mehta (EMP001): Net Pay ₹65,484, Paid Days 29
- Priya Sharma (EMP002): Net Pay ₹22,000, Paid Days 31
- **Total: ₹87,484** ✓ (matches Component Wise Breakdown total)

**Arjun's 29 paid days (out of 31):** Suggests 2 days of LOP or joining in middle of month. May 2026 has 31 calendar days; Arjun has 29 paid days — 2 days LOP or leave without pay.

---

### 7. Expected Component Drill-Down Behaviour (Not Tested)

When clicking a component row (e.g., "Basic"):
- Expected: Slide-out panel or new page showing employee-wise breakdown
- Columns expected: Employee Name | Employee ID | Basic Amount | Paid Days | Proration Factor
- Editability: In a PAID pay run — read-only. In Draft — may be editable.

---

### 8. What's Missing from Insights

| Expected Section | Observed |
|------------------|---------|
| Deductions Breakdown | Not shown (0 deductions) |
| Tax Summary | Not in Overall Insights (in Taxes & Deductions tab) |
| Loan Deductions | Not shown |
| Reimbursement Totals | Not shown |
| Donations | Shown as ₹0.00 in header summary |

---

## Screenshots / Files

- `payrun-overall-insights.png` — Overall Insights tab full view

---

## Gaps / Open Questions

- [ ] **Component drill-down click behaviour:** What happens when you click "Basic" or "HRA" row? Does a per-employee breakdown open?
- [ ] **"Employees Involved" column:** For this pay run it's 2. Would it show 1 for components only assigned to one employee's salary structure?
- [ ] **Deductions Component Wise Breakdown:** When PF/ESI/PT are active, would deductions appear in a separate section here?
- [ ] **Arjun's 29 paid days:** Why 29 when May has 31 days? Is there a 2-day LOP? Or is the "base days" calculation different from calendar days?
- [ ] **Component editing from Overall Insights:** In a Draft pay run, can individual component amounts be edited from this view or only from Employee Summary?
- [ ] **Statutory Summary with active PF/ESI:** What does the Statutory Summary section look like when PF and ESI are properly configured?
