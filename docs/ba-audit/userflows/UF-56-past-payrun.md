# UF-56: Past Pay Run Access and Historical View

**Module:** Pay Runs > Pay Run List > Historical Pay Runs
**Tested:** 2026-05-16
**Mock Data Used:** May 2026 pay run (PAID); April 2026 pay run (if exists)
**App State Before:** Pay Runs list showing May 2026 as most recent

---

## Past Pay Run Overview

Past pay runs are finalized pay runs from prior months. They are viewable but immutable — no editing allowed after finalization.

---

## Pay Runs List — Historical View

### Navigation
Pay Runs (left sidebar) → Shows all pay runs in reverse-chronological order

### List Columns (Expected)
| Column | Description |
|--------|-------------|
| Pay Period | Month and Year (e.g., "May 2026") |
| Pay Run Type | Regular / Off-Cycle / Bonus / Arrears / FnF |
| Pay Date | Date salaries were disbursed |
| Total Amount | Sum of net pay for all employees |
| Status | Draft / Under Review / Paid / Cancelled |
| Actions | View / Download |

### May 2026 Pay Run (Most Recent — Confirmed PAID)
| Field | Value |
|-------|-------|
| Pay Period | May 2026 |
| Pay Date | 29/05/2026 |
| Total (Net Pay) | ₹87,484 |
| Status | PAID |
| Employees | 2 Active, 3 Skipped |

---

## Accessing a Past Pay Run

### Step 1: Navigate to Pay Runs
Left sidebar → Pay Runs

### Step 2: Select Month
Click on a historical pay run row → Opens pay run summary

### Step 3: View Historical Data
All 3 tabs available in read-only mode:
- Summary tab: Employee-wise pay breakdown
- Overall Insights tab: Aggregate stats
- Taxes & Deductions tab: Statutory deductions

### Step 4: Download Options
From pay run header dropdown (PAID state):
- Download all Payslips
- Download all TDS Worksheets
- Show Downloads
- Delete Recorded Payment (if applicable)

---

## What Is Immutable in Past Pay Runs

| Item | Can Admin Change? |
|------|------------------|
| Employee salaries | No — use revision run |
| LOP days | No — finalized |
| Variable inputs | No — finalized |
| Payment date | No — recorded |
| Payslip content | No — frozen at finalization |
| TDS amounts | No — frozen; visible in TDS Liabilities |
| Employee list (add/remove) | No — use off-cycle run |

**Only way to correct a past pay run:** Create a Revision Pay Run (UF-57) which generates a correcting entry.

---

## April 2026 Pay Run

The demo org was likely set up after April 2026 or April 2026 pay run may not exist (demo org setup mid-month). If April 2026 exists in the list, it would confirm:
- Whether Prior Payroll was entered for FY2026-27 start
- Whether onboarding was complete enough for April pay run

---

## Pay Run History for Audit

### Audit Use Cases
1. **Compliance audit**: Verify PF/ESI/TDS deductions for prior months
2. **Employee dispute**: Pull individual payslip for disputed month
3. **Form 24Q filing**: Reference quarterly TDS data from past pay runs
4. **Bank reconciliation**: Match bank transfer with pay run total

### Download Artifacts Available
| Artifact | Format | Content |
|----------|--------|---------|
| Payslip (per employee) | PDF | Full pay breakdown |
| All Payslips (bulk) | ZIP of PDFs | All employee payslips |
| TDS Worksheet | PDF/Excel | TDS computation per employee |
| Bank Advice | Excel/CSV | Bank transfer file |

---

## Business Rules
1. Past pay runs are read-only after finalization
2. All historical pay runs retained indefinitely (no auto-deletion)
3. Payslips accessible from past pay run summary
4. TDS data from past runs feeds into Form 24Q (quarterly TDS return)
5. "Delete Recorded Payment" — may exist on PAID runs to revert to pre-payment state (see UF-47)

## Gaps / Observations
- April 2026 pay run existence not confirmed
- Pay run list UI layout not directly observed (only navigated to May 2026 run)
- Pagination of pay run list (for orgs with years of history) not explored

## Open Questions
- [ ] How far back does Zoho retain pay run history?
- [ ] Can admin filter pay run list by type (Regular vs Off-Cycle vs Bonus)?
- [ ] Is there a search function in the pay run list?
- [ ] If "Delete Recorded Payment" is used on a past run, does it cascade to affect Form 24Q data?
