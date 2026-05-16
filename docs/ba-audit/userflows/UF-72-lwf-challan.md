# UF-72: Labour Welfare Fund Challan

**Module:** Taxes & Forms > LWF (if exists) / Reports > Statutory Reports > LWF
**Tested:** 2026-05-16
**Mock Data Used:** Demo org; employees in Kerala
**App State Before:** LWF configuration not confirmed in Settings

---

## LWF Overview

Labour Welfare Fund (LWF) is a state-level statutory contribution for the welfare of labour. Governed by respective state LWF Acts.

### Key Characteristics
- **Not applicable in all states** — each state has its own LWF Act
- **Contribution**: Both employee and employer contribute
- **Frequency**: Varies by state (monthly, half-yearly, or annual)
- **Managed by**: State Labour Welfare Board

---

## Kerala LWF

### Applicability
Kerala Labour Welfare Fund Act, 1975
- Applies to establishments with 5+ employees

### Contribution Rates (Kerala)
| Party | Annual Contribution |
|-------|-------------------|
| Employee | ₹4 per year |
| Employer | ₹8 per year (2× employee) |
| Total | ₹12 per establishment-employee |

**Deduction Frequency:** Annual (typically deducted in December or as per state board notification)

### Demo Org — LWF Status
| Employee | LWF Applicable | Employee Contribution | Employer Contribution |
|----------|---------------|----------------------|----------------------|
| Arjun Mehta | Likely Yes | ₹4/year | ₹8/year |
| Priya Sharma | Likely Yes | ₹4/year | ₹8/year |

**Total Annual LWF (Demo Org):** ₹8 employee + ₹16 employer = ₹24

---

## LWF Rates — Common States

| State | Employee | Employer | Total | Frequency |
|-------|----------|----------|-------|-----------|
| Maharashtra | ₹6/month | ₹18/month | ₹24/month | Monthly (June + Dec in practice) |
| Karnataka | ₹10/year | ₹20/year | ₹30/year | Annual (December) |
| Kerala | ₹4/year | ₹8/year | ₹12/year | Annual |
| Andhra Pradesh | ₹2/half-year | ₹5/half-year | ₹7/half-year | Half-Yearly |
| Tamil Nadu | ₹10/year | ₹20/year | ₹30/year | Annual |
| Delhi | Not applicable | — | — | — |
| Gujarat | Not applicable | — | — | — |

**Note:** LWF is NOT applicable in several states including Delhi, Gujarat, Rajasthan, UP, Bihar.

---

## LWF Challan Generation Flow (Expected)

### Pre-requisites
| Requirement | Status |
|------------|--------|
| State LWF Act applicable | Yes (Kerala) |
| LWF Registration (if required) | Not confirmed |
| LWF deduction month reached | Typically December |

### Expected Navigation
`Taxes & Forms > LWF > [Year] > Generate Challan`
OR
`Reports > Statutory Reports > LWF Report` → Download

### Steps (Expected)
1. Navigate to LWF section (Taxes & Forms or Reports)
2. Select contribution year
3. View employee-wise LWF contribution table
4. Click "Generate Challan" or "Download LWF Statement"
5. Pay to Kerala Labour Welfare Board

---

## LWF Payment Process

1. Download LWF challan from Zoho
2. Log into state Labour Welfare Board portal (or visit bank)
3. Pay using challan reference
4. Retain payment receipt

**Kerala LWF Board:** Kerala Labour Welfare Board, Thiruvananthapuram
**Due Date:** As notified by state board (typically 15th January for December deduction year)

---

## LWF in Payslip

For states with monthly LWF (e.g., Maharashtra ₹6/month employee):
- Appears as deduction line: "Labour Welfare Fund: ₹6"
- Shows under Statutory Deductions on payslip

For annual LWF (Kerala ₹4/year):
- Deducted in one month (December)
- Payslip shows ₹4 in December, ₹0 all other months

---

## LWF Report (from UF-79)

The LWF statutory report includes:
- Employee-wise LWF contributions
- Employer contributions
- Total LWF liability for the year
- State-wise breakdown (if multi-state)

---

## Business Rules
1. LWF is state-specific — check applicability for each work location
2. Kerala: ₹4 employee + ₹8 employer annually
3. Deducted in payroll for the applicable month (December for Kerala)
4. Employer LWF is a cost above and beyond salary
5. LWF is not part of TDS computation
6. LWF is deductible u/s 36(1)(iv) for employer (business expense)

## Gaps / Observations
- Taxes & Forms > LWF section URL not confirmed
- LWF deduction configuration on employee profile not verified
- December 2026 pay run not yet available — LWF deduction not tested
- 🟡 LWF amounts are very small (₹4 employee) — compliance risk is reputational not financial

## Open Questions
- [ ] Where is LWF configured in Zoho Payroll? Is it in Settings > Organisation or per-employee?
- [ ] Does Zoho auto-detect state from work location and apply correct LWF rates?
- [ ] Is there a "LWF Registration Number" field in Settings?
- [ ] For states where LWF is not applicable, does Zoho simply hide the module?
