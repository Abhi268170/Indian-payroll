# UF-53: Bonus Pay Run

**Module:** Pay Runs > Create Pay Run > Bonus / Off-Cycle
**Tested:** 2026-05-16
**Mock Data Used:** Off-Cycle pay run observed (UF-52); Bonus run assumed similar
**App State Before:** May 2026 pay run PAID

---

## Bonus Pay Run Overview

A Bonus Pay Run is a separate pay run created outside the regular monthly cycle specifically for bonus disbursement. It allows the employer to pay a bonus (performance, annual, festival, etc.) independently of the regular salary.

---

## Bonus in Zoho — Two Mechanisms

### Mechanism 1: Bonus Component in Regular Pay Run
- Add bonus amount to employee's salary in the regular pay run via Variable Input
- Bonus appears on regular monthly payslip
- TDS computed on enhanced salary for that month
- Used for small, ad-hoc bonuses

### Mechanism 2: Standalone Bonus Pay Run (Off-Cycle)
- Create a separate pay run specifically for bonus
- Generate separate payslip showing only bonus
- TDS on bonus handled in this run
- Used for large annual bonuses (e.g., Diwali bonus, performance bonus)

---

## Creating a Bonus Pay Run (Expected Flow)

### Entry Point
Pay Runs > "Create Pay Run" button (or "+" button)

### Step 1: Select Pay Run Type
Options (from UF-52 off-cycle observation):
- Regular (monthly — auto-created)
- Off-Cycle / Bonus / One-Time

### Step 2: Configure Bonus Run
| Field | Value |
|-------|-------|
| Pay Run Type | Bonus / Off-Cycle |
| Pay Period | Month (e.g., May 2026) |
| Pay Date | Date bonuses will be paid |
| Description | "Annual Bonus 2026", "Diwali Bonus 2026" |
| Include Employees | Select employees receiving bonus |

### Step 3: Enter Bonus Amounts
For each included employee:
- Enter bonus amount in "Bonus" variable component field
- System computes TDS on the bonus income

### Step 4: Review and Finalize
- Review total bonus payout
- Verify TDS computation
- Mark as paid

### Step 5: Payslip Generated
Separate bonus payslip shows:
```
Earnings:
  Bonus: ₹50,000
Deductions:
  TDS: ₹XX
Net Bonus Pay: ₹50,000 - ₹XX
```

---

## TDS on Bonus

### Computation Method
TDS on bonus follows the spreading method:
1. Add bonus to annual income projection
2. Compute revised annual tax
3. Additional TDS = Revised annual tax − Tax already deducted in prior months
4. Deduct this additional TDS in the bonus month

**Example (Arjun Mehta, April–May TDS = ₹0 due to declaration lock):**
- Annual Salary: ₹7,65,000
- Bonus: ₹50,000
- Revised Annual Income: ₹8,15,000
- Tax on ₹8,15,000 (new regime): ₹1,500 + (₹15,000 × 10%) = ₹3,000 + cess
- TDS to deduct in bonus run: ₹3,000+

### Flat Rate on Bonus (Alternative)
Some systems compute 30% TDS flat on bonus (for high-value bonuses). Zoho likely uses the spreading method.

---

## Payment of Bonus Act, 1965

**Statutory Bonus:**
- Applicable: Establishments with 20+ employees
- Eligibility: Employees earning ≤ ₹21,000/month
- Minimum bonus: 8.33% of annual wages (or ₹7,000 × months, whichever is higher)
- Maximum bonus: 20% of annual wages
- Due date: Within 8 months of close of accounting year

**For demo org employees (all earning > ₹21,000):**
- Not covered by Payment of Bonus Act mandatory provisions
- Ex-gratia bonus can still be paid voluntarily

---

## Business Rules
1. Bonus pay run is an off-cycle run — does not affect regular monthly pay run
2. TDS on bonus computed via spreading across remaining FY months
3. Bonus payslip is separate from regular payslip
4. Payment of Bonus Act mandatory for establishments with 20+ employees
5. Bonus paid post-FnF (exit) may require separate processing

## Gaps / Observations
- Bonus pay run not directly tested — modeled from UF-52 off-cycle run observation
- Whether Zoho has a specific "Bonus Run" type vs generic off-cycle not confirmed
- Payment of Bonus Act compliance reporting not explored

## Open Questions
- [ ] Is "Bonus Pay Run" a distinct type in Zoho, or is it just an off-cycle run with bonus component?
- [ ] Does Zoho track the Payment of Bonus Act eligibility automatically?
- [ ] Can admin include some but not all employees in a bonus run?
- [ ] Does the bonus payslip PDF look different from a regular payslip?
