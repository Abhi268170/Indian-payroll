# UF-71: Professional Tax Challan Generation

**Module:** Taxes & Forms > Professional Tax
**Tested:** 2026-05-16
**Mock Data Used:** Demo org; Head Office = Kerala; May 2026 pay run PAID
**App State Before:** PT configured for Kerala; PT Number field blank

---

## Professional Tax Overview

Professional Tax (PT) is a state-level tax on employment income. Governed by respective state PT Acts.

### Kerala PT — Demo Org Context
- **Applicable Act:** Kerala Panchayat Raj Act / Kerala Municipality Act
- **PT Registered Location:** Head Office (Kerala)
- **PT Number:** BLANK (not configured — 🔴 gap)
- **Deduction Cycle:** Half-Yearly (September and March)

---

## Kerala PT Slabs (FY2026-27)

| Monthly Salary Range | PT per Half-Year |
|---------------------|-----------------|
| Up to ₹11,999 | ₹0 |
| ₹12,000 – ₹17,999 | ₹120 |
| ₹18,000 – ₹29,999 | ₹180 |
| ₹30,000 – ₹44,999 | ₹300 |
| ₹45,000 – ₹59,999 | ₹450 |
| ₹60,000 – ₹74,999 | ₹600 |
| ₹75,000 – ₹99,999 | ₹750 |
| ₹1,00,000 and above | ₹1,250 |

**Note:** Kerala deducts PT twice a year — September (for April–September) and March (for October–March).

### Demo Org Employees
| Employee | Monthly Gross | PT Slab | PT per Half-Year |
|----------|--------------|---------|-----------------|
| Arjun Mehta | ₹65,484 | ₹60,000–₹74,999 | ₹600 |
| Priya Sharma | ₹22,000 | ₹18,000–₹29,999 | ₹180 |

**PT deduction months:**
- September 2026 pay run: Arjun ₹600, Priya ₹180 = Total ₹780
- March 2027 pay run: Arjun ₹600, Priya ₹180 = Total ₹780
- All other months: ₹0

---

## PT Challan Generation Flow

### Pre-requisites
| Requirement | Status |
|------------|--------|
| PT registration number | BLANK — 🔴 challan blocked |
| Pay run finalized for PT month | Sept/March pay run needed |
| Employee PT enabled | Confirmed for both employees |

### Expected Navigation
`Taxes & Forms > Professional Tax > [Month] > Generate Challan`

### Steps (Expected)
1. Navigate to Taxes & Forms > Professional Tax
2. Select applicable month (September or March)
3. View PT liability table: Employee list, PT amount per employee
4. Click "Generate Challan" or "Download PT Challan"
5. System generates challan PDF with:
   - PT registration number (BLANK — blocked)
   - Employer details
   - Employee count and total PT amount
   - Due date
6. Pay challan to local PT authority

### PT Payment Due Date
- Kerala: No uniform statewide deadline — depends on local body (panchayat/municipality)
- Typically: Within 15 days of the deduction month end
- September PT → Due by 15th October
- March PT → Due by 15th April

---

## PT Challan PDF Content (Expected)

| Field | Value |
|-------|-------|
| PT Registration Number | [Blank — gap] |
| Employer Name | Lerno (demo org) |
| Period | April–September or October–March |
| Employee Count | 2 (Arjun + Priya) |
| Total PT Amount | ₹780 (September), ₹780 (March) |
| Payment Date | Date of payment |

---

## PT Reports (from UF-79)

| Report | Content |
|--------|---------|
| PT Summary Report | Month-wise PT per location |
| Employee PT Details Report | Employee-wise PT deductions |
| Annual PT Report | Full year PT by employee |

---

## Multi-State PT Considerations

If an organization has employees in multiple states:
- Each state has its own PT Act, slabs, and deduction cycle
- Zoho handles state-wise PT based on employee work location
- Each work location requires its own PT registration number
- PT challan generated per state/location

### Common State PT Deduction Cycles
| State | Cycle |
|-------|-------|
| Karnataka | Monthly |
| Maharashtra | Monthly |
| Kerala | Half-Yearly (Sept, March) |
| Andhra Pradesh | Monthly |
| Tamil Nadu | Monthly (for > ₹21,000/month) |
| West Bengal | Monthly |

---

## Business Rules
1. PT is state-specific — slabs and cycles vary by state
2. Kerala: Half-yearly deduction in September and March
3. PT based on monthly gross salary at time of deduction
4. PT registration number mandatory for challan generation
5. Each work location may need separate PT registration
6. PT is not part of TDS computation — it is a separate state levy
7. PT is deductible u/s 16(iii) of Income Tax Act (old regime only — not applicable in new regime)

## Gaps / Observations
- PT registration number is BLANK — challan cannot be generated
- 🔴 Missing PT number means employer cannot file PT returns or pay PT challan
- Taxes & Forms > Professional Tax URL not confirmed via navigation
- September 2026 pay run not yet available (date-gated)

## Open Questions
- [ ] Where is the PT registration number entered? Is it in Settings > Organisation > Tax Details?
- [ ] Can Zoho generate PT challan for a month where PT = ₹0?
- [ ] If employee changes work location mid-year, how is PT handled for partial year?
- [ ] Does Zoho generate the PT return (Form) or only the challan for payment?
