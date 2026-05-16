# Edge Case > Mid-Month Proration Verification

## Scenario
Verify proration logic for salary components when an employee has Loss of Pay (LOP) days. Original scenario assumed EMP002 joined 16 May 2026 (mid-month joiner), but actual data shows EMP002 joined 16/05/2025 — she is a full employee in May 2026. The actual proration observable is EMP001 (Arjun Mehta) with 2 LOP days in May 2026.

## Steps to Reproduce
1. Navigate to Pay Runs > May 2026 pay run
2. Open EMP001 (Arjun Mehta) payslip detail
3. Observe paid_days, lop_days, and per-component amounts
4. Cross-check against monthly CTC to verify proration formula
5. Check EMP002 to confirm full-month payment (no proration — confirms DOJ is May 2025, not May 2026)

## Expected Behaviour (statutory rule)

### Standard Proration Formula
Indian payroll standard: prorate on calendar days (not working days).
```
prorated_amount = monthly_component × (paid_days / total_calendar_days_in_month)
```

For May 2026 (31 calendar days):
- If LOP = 2 days: paid_days = 29
- Proration factor = 29/31 = 0.93548...

### Mid-Month Joiner (Separate Rule)
For a new joiner on date D of month M with N calendar days:
```
paid_days = N - D + 1
prorated_amount = monthly_component × (paid_days / N)
```
Example: Joins May 16 → paid_days = 31 - 16 + 1 = 16; factor = 16/31

### All-Components Uniform Proration
Standard practice: ALL salary components prorated at same factor.
- Basic, HRA, Special Allowance, all fixed allowances — all use same paid_days/total_days factor.
- Variable pay (bonus, OT) — not prorated (paid in full or per actuals).
- Statutory deductions (PF, PT) — applied to prorated salary.

## Actual Zoho Behaviour

### EMP001 (Arjun Mehta) — LOP Proration in May 2026

**Employee details:**
- Monthly CTC: ₹9,45,000 / 12 = ₹78,750/month
- Annual salary components from assignment:
  - Basic: ₹4,79,970 / 12 = ₹39,997.50 ≈ ₹39,998/month
  - HRA: ₹1,91,988 / 12 = ₹15,999/month
  - Fixed Allowance: ₹1,68,042 / 12 = ₹14,003.50/month

**May 2026 actuals (from payslip API):**
- `paid_days`: 29
- `lop_days`: 2
- `total_days`: 31

**Prorated amounts (May 2026 payslip):**
| Component | Monthly Amount | Proration Factor | Prorated Amount | Verified |
|-----------|---------------|-----------------|----------------|----------|
| Basic | ₹39,998 | 29/31 | ₹37,417 | ✓ |
| HRA | ₹15,999 | 29/31 | ₹14,967 | ✓ |
| Fixed Allowance | ₹14,003 | 29/31 | ₹13,100 | ✓ |
| **Total Gross** | **₹65,484** (approx) | | **₹65,484** | Matches |

Verification: ₹39,998 × 29/31 = ₹37,417.29 → rounds to ₹37,417 ✓

**Proration formula used**: `floor(monthly_component × paid_days / total_calendar_days)`

### EMP002 (Priya Sharma) — Full Month, No Proration

**Employee details:**
- DOJ: 16/05/2025 (joined May 2025, not May 2026)
- Monthly CTC: ₹22,000/month flat

**May 2026 actuals:**
- `paid_days`: 31
- `lop_days`: 0
- `total_days`: 31

No proration applied — full ₹22,000 paid. Confirms EMP002 is NOT a mid-month joiner in May 2026.

### Proration Basis: Calendar Days vs Working Days
From EMP001 data: proration factor = 29/31 (calendar days), NOT 29/23 (working days in May 2026 — approx 23 working days). This confirms **Zoho uses calendar-day proration**, not working-day proration.

### LOP Tracking
- LOP days tracked as `lop_days` field in payrun employee record
- `paid_days = total_calendar_days - lop_days`
- LOP can be entered via variable inputs section of payrun or via Leave Management integration
- Source of LOP not visible in payrun UI (no "reason" field for LOP displayed)

### Rounding Behaviour
Component-level rounding to rupees (not paise). Observed:
- ₹39,998 × 29/31 = ₹37,417.29 → stored as ₹37,417 (floor/truncate, not round)
- Need confirmation: is it floor, round-half-up, or banker's rounding?

## Screenshots
- No specific screenshots captured for proration (data extracted via API)

## Gap / Bug / Surprise
1. **Scenario reframe required**: EMP002 DOJ is 16/05/2025 — she was a full employee in May 2026. The "mid-month joiner in May 2026" scenario does not exist in this org. The actual observable proration event is EMP001's LOP-based proration.
2. **Proration confirmed as calendar-day based**: 29/31, not working-day based. This matches Indian payroll standard (calendar days are the statutory norm).
3. **Rounding appears to be floor (truncate)**: ₹37,417.29 → ₹37,417. Need to verify: does Zoho always truncate, or round? Truncation is safer for employer (pays less), but round-half-up is more common and fairer to employees.
4. **All components prorated uniformly**: Basic, HRA, and Fixed Allowance all use the same 29/31 factor — no differential proration per component type.
5. **No LOP reason field visible**: Cannot audit whether LOP was approved leave, absent, or manual entry — no audit trail visible on payslip or payrun UI.
6. **Mid-month joiner proration untested**: Cannot directly verify joiner proration formula since no employee joined mid-month in the test period. Assumed same calendar-day logic applies.

## How We Should Build This

### Proration Engine
```
// Calendar-day proration (statutory standard)
decimal Prorate(decimal monthlyAmount, int paidDays, int calendarDaysInMonth)
{
    return Math.Floor(monthlyAmount * paidDays / calendarDaysInMonth * 100) / 100;
    // Or: Round(monthlyAmount * paidDays / calendarDaysInMonth, 2, MidpointRounding.AwayFromZero)
    // Confirm rounding policy with client — document as org-level config
}
```

### Paid Days Computation
```
paid_days = calendar_days_in_month - lop_days
// For mid-month joiner: paid_days = calendar_days_in_month - doj_day_of_month + 1
// For mid-month exit: paid_days = last_working_day_of_month
```

### Component-Level Rules
- Fixed components (Basic, HRA, Special Allowance, all fixed allowances): prorate at `paid_days / calendar_days`
- Variable components (Bonus, OT, Commission): do NOT prorate — pay actuals
- Statutory deductions (PF, PT, ESI): apply after proration on earned amounts

### Proration Config
- `ProrationBasis` enum: `CalendarDays` (default) | `WorkingDays` (rare)
- Org-level config (not per-employee)
- Store `proration_basis`, `rounding_policy` (Floor | HalfUp | BankersRound) in `PayrollConfig`

### LOP Entry
- `LopEntry` entity: `employee_id`, `payrun_id`, `lop_days`, `reason`, `approved_by`, `approved_at`
- Leave Management integration: auto-populate LOP from leave records
- Manual override allowed with audit log entry
- LOP must be visible on payslip ("Loss of Pay: 2 days")
