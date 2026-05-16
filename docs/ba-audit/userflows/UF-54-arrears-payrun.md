# UF-54: Arrears Pay Run

**Module:** Pay Runs > Create Pay Run > Arrears
**Tested:** 2026-05-16
**Mock Data Used:** Arjun Mehta pending salary revision ₹70,000 → ₹78,750 (effective date unknown)
**App State Before:** May 2026 pay run PAID; Arjun's revision pending

---

## Arrears Pay Run Overview

An **Arrears Pay Run** is used to pay the difference in salary that was owed from a prior period due to:
1. **Salary revision applied retroactively** — e.g., revision approved in June but effective from April
2. **Missed pay** — employee was skipped in a prior month but should have been paid
3. **Component addition retroactively** — new allowance added effective from a prior date
4. **Settlement of prior disputes** — salary withheld and now released

---

## Arrears Trigger — Arjun's Salary Revision

From UF-81 (Salary Revisions report observation):
| Detail | Value |
|--------|-------|
| Employee | Arjun Mehta |
| Current CTC | ₹70,000/month (approx) |
| Revised CTC | ₹78,750/month (approx) |
| Revision Status | Pending |
| Effective Date | Not confirmed |

**Scenario:** If Arjun's revision is effective from 01/04/2026 (start of FY), but the revision is only approved in June 2026:
- April and May 2026 were paid at old rate (₹70,000/month)
- Arrears due: (₹78,750 − ₹70,000) × 2 months = ₹17,500

---

## Arrears Calculation

### Component-Level Arrears
Each salary component is re-computed at the revised rate for prior months:

| Component | Old | New | Monthly Arrear |
|-----------|-----|-----|----------------|
| Basic | ₹40,000 | ₹45,000 | ₹5,000 |
| HRA | ₹20,000 | ₹22,500 | ₹2,500 |
| Fixed Allowance | ₹13,100 | ₹14,300 | ₹1,200 |
| Conveyance | — | — | — |
| **Total/month** | ₹70,000 | ₹78,750 | ₹8,750 |

**2 months arrears (April + May):** ₹17,500

---

## Arrears and TDS

Arrears are taxable income for the year they are received (Section 192).

### Spread Across Remaining Months
Arrears income is added to taxable income and TDS is recalculated:
- Prior months: TDS already deducted at old rate
- Current month: TDS adjusted to cover arrears income

### Section 89(1) Relief
Employees can claim Section 89(1) relief if arrears received in current FY relate to prior FY:
- File Form 10E with Income Tax Portal
- Reduces TDS on arrears if marginal rate was lower in the year of earning
- Zoho may show TDS without 89(1) relief; employee claims it at ITR filing

---

## Creating Arrears Pay Run (Expected Flow)

### Entry Point
Pay Runs > "Create Pay Run" > Select "Arrears" type

OR: When salary revision is approved with a backdated effective date, Zoho may auto-prompt: "Arrears pending for X months. Create arrears run?"

### Step 1: Select Arrears Type
- Salary revision arrears (auto-computed based on revision)
- Manual arrears (admin enters amounts)

### Step 2: Select Period
- From Month: April 2026
- To Month: May 2026
- (For 2 months of arrears)

### Step 3: Select Employees
- Arjun Mehta (revision applicable)
- Other employees if revision affects multiple

### Step 4: System Computes Arrears
| Employee | Period | Component | Arrear Amount |
|----------|--------|-----------|---------------|
| Arjun | April 2026 | Basic | ₹5,000 |
| Arjun | April 2026 | HRA | ₹2,500 |
| Arjun | May 2026 | Basic | ₹5,000 |
| Arjun | May 2026 | HRA | ₹2,500 |
| **Total** | | | **₹17,500** |

### Step 5: Review and TDS Computation
- System shows revised TDS based on arrears income
- Admin reviews and finalizes

### Step 6: Mark as Paid
- Arrears pay run → PAID
- Separate payslip generated showing arrears
- TDS deducted in this run

---

## Arrears Payslip

```
Arrears Pay Run — June 2026

Arrears (April 2026 – May 2026):
  Basic Arrears: ₹10,000
  HRA Arrears: ₹5,000
  Fixed Allowance Arrears: ₹2,500

Deductions:
  TDS on Arrears: ₹XXX

Net Arrears: ₹17,500 − ₹XXX
```

---

## Business Rules
1. Arrears are taxable in the year of receipt (not the year they pertain to)
2. Arrears TDS computed by spreading over remaining months
3. Section 89(1) relief available — employee must file Form 10E
4. Arrears payslip is separate from regular payslip
5. Statutory deductions (PF, ESI) are NOT computed on arrears (only on regular salary)

## Gaps / Observations
- Arjun's revision effective date not confirmed — arrears period unknown
- Arrears run creation not tested (revision still pending approval)
- 🟡 Test after approving Arjun's revision with backdated effective date

## Open Questions
- [ ] Does Zoho auto-detect backdated revision and prompt arrears run creation?
- [ ] Are PF contributions calculated on arrears amounts?
- [ ] Does Zoho provide a Form 10E download for Section 89(1) relief?
- [ ] Can admin manually enter custom arrears (not tied to a salary revision)?
