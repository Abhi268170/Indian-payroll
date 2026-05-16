# UF-42: Add LOP in Pay Run

**Module:** Pay Runs > Run Payroll / Pay Run initiation
**Tested:** 2026-05-16
**Mock Data Used:** Attempted June 2026 Regular Pay Run creation
**App State Before:** May 2026 pay run = PAID; no active pay run in queue

## Steps Executed
1. Navigate to `#/payruns` (Run Payroll tab)
2. Observe "You have no outstanding pay runs" — empty state
3. Click "New" dropdown — observe options: One Time Payout, Off Cycle Payrun
4. Note: Regular Pay Run for June 2026 is NOT available in the "New" dropdown
5. Navigate to Payroll History — only April and May 2026 show as Paid
6. Conclude: June 2026 regular pay run is date-gated (only available from 01/06/2026 or later)

## Key Finding: Regular Pay Run Is Date-Gated

The June 2026 Regular Pay Run **cannot be created on 2026-05-16** (current date). Zoho Payroll auto-creates the regular pay run in the "Run Payroll" queue at the start of each calendar month. The pay run appears only when the current date falls within or after the pay period.

This means:
- **UF-42 (LOP), UF-43 (Variable Pay), UF-44 (Reimbursement), UF-45-48 (Approval), UF-49 (Mark as Paid)** cannot be fully executed today
- The below documents the intended flow based on what was observable from the May 2026 PAID pay run interface

## LOP Input Flow (Documented from UI Patterns)

Based on investigation of the May 2026 pay run (where Arjun had 2 LOP days), LOP is entered **before** the pay run is finalized. The entry point is within the active (unfinalized) pay run.

### Where LOP Is Entered

In an active Regular Pay Run (status: Draft/Processing):
- Navigate to the Employee Summary tab
- Each employee row shows an "Edit" or "LOP" action in the overflow/actions column
- LOP days are entered per employee as a numeric input
- The LOP value feeds directly into proration formula

### LOP Input Field
| Field | Type | Notes |
|-------|------|-------|
| LOP Days | Number (integer) | Enter days absent without pay |
| Payable Days | Auto-calculated | = Base Days − LOP Days |

### LOP Calculation (Observed from Arjun's May 2026 Data)
- Base Days: 31 (May calendar days)
- LOP: 2
- Payable Days: 29
- Proration factor: 29/31
- Effect on each component: `Component × 29/31`

### Net Pay Impact (₹70,000 salary, 2 LOP days, May):
| Component | Full Month | Prorated (29/31) |
|-----------|-----------|-----------------|
| Basic | ₹40,000 | ₹37,419 |
| HRA | ₹16,000 | ₹14,968 |
| Fixed Allowance | ₹14,000 | ₹13,097 |
| **Total** | **₹70,000** | **₹65,484** |

Loss per LOP day = ₹70,000 / 31 = ₹2,258/day
2 LOP days = ₹4,516 deducted

### LOP Effect on Statutory Deductions
- PF wage is prorated if "Consider applicable salary components based on LOP" = Yes AND PF wage < ₹15,000 (per EPF config)
- PT: PT wage basis is gross pay — LOP reduces gross, so PT slab may shift
- TDS: Reduced monthly gross reduces annualized projected income → lower TDS

## Pay Run New Options Documented

From the "New" button dropdown on `#/payruns`:
| Option | What It Creates |
|--------|----------------|
| One Time Payout | Ad-hoc payment for specific employees outside regular cycle |
| Off Cycle Payrun | Mid-month or supplementary pay run (custom pay date) |

The Regular Pay Run is auto-created by the system at the beginning of each month — no "New > Regular Payroll" button exists.

## Off-Cycle Pay Run — Dialog Fields

From the "Initiate Off Cycle Pay Run" modal:
| Field | Type | Required | Notes |
|-------|------|----------|-------|
| When would you like to pay? | Date (dd/MM/yyyy) | Yes | The pay date |

Actions: "Save and Continue" / "Cancel"
Post-save: Navigates to an off-cycle pay run form (employees selectable, components editable)

## Gaps / Observations
- June 2026 LOP flow cannot be demonstrated until June 2026 begins
- No "Advance" or "Pre-create" option for upcoming month's regular pay run
- LOP entry field not visible in PAID pay run (immutable once paid) — must be done during active/draft state
- No LOP import (bulk CSV upload) visible from current UI
- 🟡 Future session required: create June pay run on or after 01/06/2026 to test LOP entry, variable pay, reimbursements, approval flow, and mark-as-paid
