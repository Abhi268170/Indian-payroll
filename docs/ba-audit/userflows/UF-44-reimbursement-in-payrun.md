# UF-44: Reimbursement Processing in Pay Run

**Module:** Pay Runs > [Month] Pay Run > Reimbursements
**Tested:** 2026-05-16
**Mock Data Used:** N/A — DATE-GATED (requires June 2026 pay run + active reimbursement claims)
**App State Before:** All reimbursement components INACTIVE in demo org; no active claims

---

## DATE-GATED / CONFIGURATION-GATED FLOW

Two pre-conditions required:
1. June 2026 regular pay run (available from 01/06/2026)
2. At least one active Reimbursement component with approved claim

**Current state:** All reimbursement components are INACTIVE (Fuel, Driver, Vehicle Maintenance, Telephone, LTA). No claims can be submitted.

**Resume this flow after:**
- Activating at least one reimbursement component in Settings > Salary Components
- Employee submitting a reimbursement claim
- Admin approving the claim
- June 2026 pay run being available

---

## Reimbursement Components (from UF-86)

| Component | Status | Max Amount | Type |
|-----------|--------|------------|------|
| Fuel & Travel Reimbursement | INACTIVE | ₹0 | Reimbursement |
| Driver Reimbursement | INACTIVE | ₹0 | Reimbursement |
| Vehicle Maintenance Reimbursement | INACTIVE | ₹0 | Reimbursement |
| Telephone Reimbursement | INACTIVE | ₹0 | Reimbursement |
| Leave Travel Allowance | INACTIVE | ₹0 | Reimbursement |

**All maximum amounts are ₹0** — this means even if activated, zero reimbursement payout is configured.

---

## Reimbursement in Pay Run — Expected Flow

### Step 1: Employee Submits Claim (Employee Portal)
- Employee logs into portal
- Navigates to Reimbursements
- Submits claim with bill amount, date, category, attachment

### Step 2: Admin Approves Claim (Approvals Module)
- Admin sees claim in Approvals > Reimbursements
- Reviews, approves/rejects
- Approved amount: can be equal to or less than claimed amount
- Selects payout month: the pay run month in which the reimbursement is paid

### Step 3: Reimbursement Appears in Pay Run
- When the payout month's pay run is processed:
  - Approved reimbursement amount appears as a line item in employee's pay
  - Added to gross pay for the month

### Step 4: Payslip Shows Reimbursement
Payslip displays:
```
Reimbursements:
  Fuel Reimbursement: ₹2,500
Total Reimbursements: ₹2,500
```

---

## Taxability of Reimbursements

| Type | Taxability |
|------|-----------|
| Actual expense reimbursement with bills | Tax-free (actual cost recovery) |
| Reimbursement exceeding actual expense | Taxable as perquisite |
| LTA | Exempt u/s 10(5) — 2 journeys in 4-year block, economy fare limit |
| Telephone | Exempt if for official use (with bills) |

**In New Tax Regime:** Most reimbursement exemptions NOT available (except very few like LTA is debated post-FY2024 amendments).

---

## Business Rules
1. Reimbursement only paid in approved payout month — not auto-carried to next month
2. Approved amount ≤ claimed amount
3. Claims rejected cannot be resubmitted (must re-create claim)
4. LTA: Maximum 2 claims per 4-year block; actual travel required
5. Reimbursement max amount per component set in Settings (currently ₹0 for all)
6. Employer must maintain documentation (bills) for audit

## Gaps / Observations
- All reimbursement components INACTIVE — no live testing possible
- Max amounts all ₹0 — configuration incomplete in demo org
- 🟡 To test: Activate "Fuel & Travel Reimbursement", set max amount, test full claim-approve-payout cycle

## Open Questions
- [ ] Is there a bulk approval option for multiple reimbursement claims?
- [ ] What happens if employee submits ₹3,000 claim but component max is ₹2,000?
- [ ] Can admin directly add a reimbursement to a pay run without employee submission?
- [ ] Does the system track the 4-year LTA block automatically?
