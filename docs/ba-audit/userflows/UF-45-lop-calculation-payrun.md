# UF-45: LOP (Loss of Pay) Calculation in Pay Run

**Module:** Pay Runs > Employee Summary > Edit LOP / Attendance
**Tested:** 2026-05-16
**Mock Data Used:** Arjun Mehta — May 2026 pay run; LOP Days = 2; Payable Days = 29 of 31
**App State Before:** May 2026 pay run PAID

---

## LOP Confirmed in May 2026 Pay Run

From UF-50 (payslip observation):
- **Payable Days:** 31 (calendar days)
- **LOP Days:** 2
- **Actual Payable Days:** 29

### Effect on Arjun's Pay
| Component | Full Month (31 days) | After LOP (29/31 days) | Actual |
|-----------|---------------------|----------------------|--------|
| Basic | ₹40,000 | ₹40,000 × 29/31 = ₹37,419 | ₹37,417* |
| HRA | ₹20,000 | ₹20,000 × 29/31 = ₹18,709 | ₹14,967** |
| Fixed Allowance | — | — | ₹13,100 |

*Minor rounding difference observed
**HRA calculation differs — possibly based on different proration base or formula

**LOP Proration Formula:** `Component Amount × (Actual Payable Days / Calendar Days in Month)`

---

## How LOP Is Entered in Zoho

### Entry Point
Pay Run > Employee Row > Three-dot menu or Edit > "Edit Attendance" or "Edit LOP Days"
OR: Pay Run > Employee Row > Direct field edit

### LOP Input (Expected)
| Field | Type | Notes |
|-------|------|-------|
| LOP Days | Number | Days without pay this month |
| Present Days | Number | May auto-calculate from LOP |
| Reason | Text / Dropdown | Optional — reason for LOP |

### Auto-Recalculation
After entering LOP days:
1. System recalculates all prorated components
2. Gross salary reduces
3. TDS recalculates on lower gross
4. Net pay reduces
5. Payslip regenerated

---

## LOP and Statutory Deductions

### PF Impact of LOP
- PF wage = min(PF-eligible components, ₹15,000)
- If PF wage after LOP < ₹15,000: PF computed on actual (no cap)
- If PF wage after LOP > ₹15,000: PF still capped at ₹1,800 (12% × ₹15,000)
- NCP Days in ECR = LOP Days (Non-Contributing Period)

### ESI Impact of LOP
- ESI wage = actual gross after LOP
- If gross after LOP < ₹21,000: ESI still applies (if employee was covered)
- If gross after LOP > ₹21,000: ESI does not apply

### PT Impact of LOP
- PT based on monthly gross salary
- LOP reduces gross → may shift employee to lower PT slab

### TDS Impact of LOP
- Annual taxable income reduces (LOP month has lower salary)
- TDS recomputed: (Annual Tax − TDS Deducted So Far) / Remaining Months
- In Arjun's case: Lower gross in May → slightly lower annual projection → marginally lower TDS

---

## LOP Proration — Component Rules

Not all components are prorated:
| Component Type | Prorated on LOP? | Notes |
|----------------|-----------------|-------|
| Basic | Yes | Fixed component × payable days / calendar days |
| HRA | Yes | Fixed component × payable days / calendar days |
| Conveyance | Yes | Fixed component — prorated |
| Special Allowance | Yes | Fixed component — prorated |
| Fixed Allowance | Unclear | Arjun's ₹13,100 appears to not be prorated (same as full month) |
| Bonus | No | Variable — flat amount entered by admin |
| Gratuity | No | Variable — calculated separately |

**Observation from UF-50:** Arjun's Fixed Allowance = ₹13,100 in May (LOP 2 days). If this component were prorated: 13,100 × 29/31 = ₹12,254. The ₹13,100 suggests Fixed Allowance is NOT prorated — confirms it is treated as a flat, non-prorated amount.

---

## LOP and Attendance Integration

Zoho Payroll can integrate with Zoho People (HRMS) for attendance data:
- Zoho People tracks daily attendance (IN/OUT, leave records)
- LOP auto-populates in Payroll based on approved leave without pay from People
- Without integration: Admin manually enters LOP days in pay run

**Demo org:** No Zoho People integration observed. LOP appears to be manually entered.

---

## Business Rules
1. LOP days reduce gross salary via proration: Amount × (PayableDays / CalendarDays)
2. Fixed Allowance may be non-prorated (flat amount per configuration)
3. NCP Days in EPF ECR = LOP days for that employee
4. PT slab rechecked after LOP (lower gross may shift slab)
5. TDS recalculated after LOP (lower annual projection)
6. LOP days must be entered before payroll is finalized

## Gaps / Observations
- LOP entry UI (the exact field/modal) not directly observed — inferred from payslip
- Fixed Allowance non-proration behavior inferred from data; not confirmed from settings
- Attendance integration with Zoho People not explored

## Open Questions
- [ ] Can admin enter fractional LOP days (e.g., 0.5 day for half-day absence)?
- [ ] Is there a configuration to make Fixed Allowance non-prorated vs prorated?
- [ ] How does LOP interact with paid leave that is already approved (LOP only for unauthorized absence)?
- [ ] If LOP > payable days, what happens (can net pay go negative)?
