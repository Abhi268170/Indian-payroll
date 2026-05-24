# Edge Case > Multiple Work Locations — PT Differences

## Scenario
EMP001 and EMP002 are supposed to be in Maharashtra (Mumbai), EMP003 in Karnataka (Bangalore), to test PT slab differences. Verify correct state-wise PT slabs are applied.

## Steps to Reproduce
1. Check work location configuration for EMP001, EMP002, EMP003
2. Navigate to Statutory Components > Professional Tax settings
3. Review May 2026 payslips for PT deductions
4. Compare PT amounts against state slabs

## Expected Behaviour (statutory rule)

### Maharashtra PT Slabs (monthly basis)
| Monthly Salary | Monthly PT |
|---------------|-----------|
| Up to ₹7,500 | Nil |
| ₹7,501–₹10,000 | ₹175 |
| Above ₹10,000 | ₹200 (₹300 in February) |

### Karnataka PT Slabs (monthly basis)
| Monthly Salary | Monthly PT |
|---------------|-----------|
| Up to ₹15,000 | Nil |
| ₹15,001–₹24,999 | ₹150 |
| ₹25,000–₹35,000 | ₹200 |
| ₹35,001–₹49,999 | ₹300 |
| ₹50,000 and above | ₹200 |

### Kerala PT Slabs (Half-Yearly)
As per actual Zoho configuration for this org:
| Half-Yearly Gross Salary | PT (Half-Yearly) | Deduction Months |
|--------------------------|-----------------|-----------------|
| ₹1 – ₹11,999 | ₹0 | August, February |
| ₹12,000 – ₹17,999 | ₹320 | August, February |
| ₹18,000 – ₹29,999 | ₹450 | August, February |
| ₹30,000 – ₹44,999 | ₹600 | August, February |
| ₹45,000 – ₹99,999 | ₹750 | August, February |
| ₹1,00,000 – ₹1,24,999 | ₹1,000 | August, February |
| ₹1,25,000+ | ₹1,250 | August, February |

## Actual Zoho Behaviour

### Work Location Configuration (Critical Finding)
From API data, ALL employees have the same work location:
- **Work Location**: Head Office
- **State**: Kerala (KL)
- **PT Applicable**: Yes
- **LWF Applicable**: No

This means:
- **EMP001** (Arjun Mehta): Kerala, NOT Maharashtra as assumed in audit brief
- **EMP002** (Priya Sharma): Kerala, NOT Maharashtra as assumed in audit brief
- **EMP003** (Vikram Nair): Kerala, NOT Karnataka as assumed in audit brief

**Only one work location exists in the entire org**: Head Office (Kerala)

This invalidates the multi-location PT test — there is no multi-location setup in this org.

### PT Settings Confirmed
From `/api/v1/components/statutorycompliance/professionaltax`:
```json
{
  "state": "Kerala",
  "state_code": "KL",
  "tax_configuration_frequency": "half-yearly",
  "deduction_frequency": "half-yearly",
  "location_name": "Head Office",
  "slab_details": [
    { "start": 1, "end": 11999, "pay_amount": 0, "months": ["August","February"] },
    { "start": 12000, "end": 17999, "pay_amount": 320, "months": ["August","February"] },
    { "start": 18000, "end": 29999, "pay_amount": 450, "months": ["August","February"] },
    { "start": 30000, "end": 44999, "pay_amount": 600, "months": ["August","February"] },
    { "start": 45000, "end": 99999, "pay_amount": 750, "months": ["August","February"] },
    { "start": 100000, "end": 124999, "pay_amount": 1000, "months": ["August","February"] },
    { "start": 125000, "end": 999999999, "pay_amount": 1250, "months": ["August","February"] }
  ]
}
```

### PT Deduction in May 2026 Payrun
For all employees: **PT = ₹0**

**Reason**: Kerala PT is deducted **half-yearly** in August and February only — not monthly. May 2026 is neither August nor February, hence ₹0 PT deduction in May 2026. This is **correct** behaviour for Kerala PT.

### Expected PT for Upcoming Runs
For EMP001 (Arjun Mehta) — gross ~₹65,484/month:
- Half-yearly gross (6 months): ~₹3,92,904 (very high — but PT is assessed on monthly gross)
- Wait: Kerala PT slab uses **monthly** gross salary range despite **half-yearly** deduction frequency
- EMP001 monthly gross: ₹65,484 — falls in ₹45,000–₹99,999 slab → ₹750 per half year
- PT deducted in August: ₹750; in February: ₹750
- Annual PT: ₹1,500

For EMP002 (Priya Sharma) — gross ₹22,000/month:
- Falls in ₹18,000–₹29,999 slab → ₹450 per half year
- Annual PT: ₹900

## Screenshots
- `screenshots/109-pt-kerala-slab.png` — PT settings page showing Kerala slab configuration

## Gap / Bug / Surprise
1. **SCENARIO BLOCKER**: All employees are in Kerala — the multi-location (Maharashtra vs Karnataka) PT test cannot be performed as there is only one work location configured.
2. **PT = ₹0 in May is CORRECT**: Half-yearly Kerala PT is deducted in August and February only. This is often misunderstood — the May ₹0 is not a bug.
3. **PT slab basis ambiguity**: The Kerala PT slab shows monthly salary ranges (₹12,000–₹17,999 etc.) but the deduction is half-yearly. Zoho appears to assess PT using the monthly gross salary to determine the slab, then deduct the half-yearly amount in August/February.
4. **Female exemption**: None of the Kerala PT slabs have `is_female_exempted: true`. This is correct for Kerala (Kerala PT Act does not exempt women unlike some other states).
5. **No female exemption flag in EMP002 setup**: EMP002 (Priya Sharma, female) is correctly not exempted from PT — this is correct for Kerala.
6. **Multi-location UX**: The Payroll meta confirms only one active work location. To test Maharashtra vs Karnataka PT, new work locations would need to be added and employees reassigned.

## How We Should Build This
- `WorkLocation` entity: `state_code` (required), `pt_applicable` (bool), `lwf_applicable` (bool), `is_metro` (bool, for HRA calculation)
- `ProfessionalTaxConfig` entity: per work location, with `SlabEntry[]` (start, end, pay_amount, deduction_months[], female_exempt)
- PT assessment: Run monthly; check if current month is in the employee's slab's `deduction_months`; if yes, deduct the slab's `pay_amount`
- PT slab lookup: Use employee's current month gross salary to determine slab
- Support both monthly and half-yearly PT collection frequencies (state-specific)
- Maharashtra PT: monthly deduction (₹200 standard, ₹300 in Feb)
- Karnataka PT: monthly deduction
- Kerala PT: half-yearly deduction (August + February)
- Store PT config per work location so multi-location orgs automatically get correct state slabs
