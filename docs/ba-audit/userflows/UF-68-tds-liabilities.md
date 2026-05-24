# UF-68: TDS Liabilities Page

**Module:** Taxes & Forms > TDS Liabilities
**Tested:** 2026-05-16
**Mock Data Used:** May 2026 PAID pay run; TDS ₹0 for all employees
**App State Before:** Pay run finalized and paid; TDS sheets show ₹0 for all employees

## Steps Executed
1. Navigate to Taxes & Forms > TDS Liabilities (via sidebar)
2. Observe page layout, tabs, filters
3. Observe empty/zero state in both Unpaid and Paid tabs

---

## TDS Liabilities Page Layout

### URL Pattern
`#/taxes-forms/tds-liabilities` (or similar — exact URL not confirmed)

### Tabs
| Tab | Content |
|-----|---------|
| Unpaid | TDS deducted from employees but not yet deposited to Income Tax dept |
| Paid | TDS amounts that have been deposited / challan uploaded |

### Filters (Observed on Page)
| Filter | Type | Notes |
|--------|------|-------|
| Quarter | Dropdown | Q1/Q2/Q3/Q4 of FY |
| Month | Month picker | Filter by specific deduction month |

---

## Unpaid Tab — Current State

No TDS liabilities shown. Reason: All employees in May 2026 pay run had ₹0 TDS (Arjun's IT declaration locked, Priya below taxable threshold).

**Expected columns when TDS > 0:**
| Column | Notes |
|--------|-------|
| Month | Deduction month (e.g., May 2026) |
| No. of Employees | Count of employees with TDS |
| TDS Deducted | Total TDS amount deducted |
| Surcharge | Surcharge if applicable |
| Education Cess | 4% health and education cess |
| Total Payable | TDS + Surcharge + Cess |
| Due Date | 7th of next month (or March: 30th April) |
| Status | Unpaid / Overdue |
| Action | "Mark as Paid" / "Upload Challan" |

---

## Paid Tab — Current State

No paid TDS entries visible. Confirmed: since TDS = ₹0 in all processed pay runs, no challan has been generated.

---

## TDS Deposit Due Dates (Statutory Reference)

Per Section 200 of Income Tax Act + Rule 30 of IT Rules:
| Scenario | Due Date |
|----------|----------|
| TDS deducted by non-government deductor | 7th of following month |
| TDS for March | 30th April |
| TDS on salary paid in advance (12th month payment) | 30th April |

Example: May 2026 TDS → Due by 7th June 2026.

---

## TDS Liability Generation Trigger

TDS Liabilities are generated AFTER a pay run is finalized/approved. The system computes:
```
Monthly TDS = (Projected Annual Tax − YTD Tax Already Deducted) / Remaining Months
```

For Arjun in May 2026:
- IT Declaration LOCKED (no submission) → Projected Annual Tax = uncomputable
- System treated TDS as ₹0 (conservative approach: no declaration = no TDS computed yet)
- This is a 🔴 compliance risk — if Arjun's salary is taxable and no TDS is deducted, the employer is liable for interest under Section 201

---

## Cross-Module Effects
- TDS Liabilities feed into Form 24Q filing (quarterly TDS return)
- Once TDS is deposited and challan is uploaded, it appears in the Paid tab and is eligible for Form 24Q inclusion
- Form 16 generation requires Form 24Q to be filed — no TDS data = no Form 16

## Gaps / Observations
- 🔴 May 2026 TDS = ₹0 for Arjun despite taxable salary — IT Declaration lock causing compliance risk
- No "Mark as Paid" button tested (no active liability)
- No challan upload flow tested
- No interest/penalty calculation visible for overdue TDS
- Due date reminder / compliance calendar integration not verified from this page

## Open Questions
- [ ] When Arjun files IT declaration and TDS is computed, does it retroactively create a TDS liability for prior months?
- [ ] Does the system send a reminder when TDS is unpaid past the 7th?
- [ ] What format is the TDS challan (ITNS 281)?
- [ ] Can the admin manually create a TDS liability entry (for prior-period corrections)?
