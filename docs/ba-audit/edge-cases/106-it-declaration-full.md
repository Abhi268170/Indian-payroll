# Edge Case > IT Declaration — Full Form Audit

## Scenario
Audit every section and field of the IT Declaration form under Old Regime (With Exemptions) for EMP001 Arjun Mehta. Document all deduction categories, sub-items, limits, and the new section numbering introduced by the Finance Act 2025 (New Income Tax Code).

## Steps to Reproduce
1. Navigate to EMP001 Investments tab
2. Click "Submit Declaration" (routes to `?tax_regime=with_exemptions`)
3. Observe and document every section and field

## Expected Behaviour (statutory rule)
The IT Declaration form should capture all deductions available under Chapter VI-A of the Income Tax Act (as applicable pre-Finance Act 2025), plus HRA exemption (Section 10(13A)), home loan interest (Section 24), and other exemptions.

## Actual Zoho Behaviour

### Form Identity
- **URL**: `#/people/employees/{id}/investment-declaration/new?tax_regime=with_exemptions`
- **Title**: "Arjun's IT Declaration"
- **Period shown**: 2026–27
- **Banner text**: "Enter your planned investment declarations here and choose the desired regime in the following page."
- **Final action**: "Submit and Compare" (regime comparison) | "Cancel"

### Section A: Property & Housing Questions (Toggle Yes/No)
Three binary toggle questions at the top of the form:

| Question | Default | Opens sub-section |
|----------|---------|-------------------|
| Is the employee staying in a rented house? | No | HRA exemption details if Yes |
| Is the employee repaying home loan for a self occupied house property? | No | Home loan interest (Section 24/Section 22) if Yes |
| Is the employee receiving rental income from let out property? | No | Rental income & interest on let-out property loan if Yes |

### Section B: Other Sources of Income (Collapsible accordion)
- Accordion: "Other Sources of Income"
- From API (`other_incomes_declarations`):
  | Income Type | API Type |
  |-------------|----------|
  | Interest Paid on Home Loan | `interest_on_home_loan_self_occupied` |
  | Income from other sources | `other_incomes` |
  | Interest Earned from Savings Deposit | `savings_deposit_interest` |
  | Interest Earned from Fixed Deposit | `fixed_deposit_interest` |
  | Principal Paid on Home Loan | `principal_on_home_loan_self_occupied` |
  | Interest Earned from National Savings Certificates | `income_from_nsc_interest` |

### Section C: Section 123 Investments (New Code: formerly 80C)
- **Label**: "Section 123 Investments (Earlier: 80C Investments)"
- **Max limit**: ₹1,50,000.00 (per `section6a_80c_details.max_limit`)
- **Combined limit applies to**: 80C + 80CCC + 80CCD(1)
- **Note text**: "This section contains the list of investments including LIC schemes, mutual funds and PPF. The maximum limit for this section is ₹1,50,000.00"
- **UI**: Dropdown (investment type) + amount field + "Add an Investment" button

Sub-items available in Section 123 (80C):
| Investment Type | API key |
|----------------|---------|
| Life Insurance Premium | `lic` |
| Public Provident Fund | `ppf` |
| Unit-linked insurance plan | `ulip` |
| National Savings Certificates | `nsc` |
| ELSS Tax Saving Mutual Fund | `mutual_fund` |
| Children Tuition Fees | `tuition_fees` |
| Sukanya Samriddhi Deposit Scheme | `ssads` |
| 5 Year fixed deposit in Scheduled Banks | `fd_bank` |
| Term deposit in post office | `term_post` |
| Senior Citizen Savings Scheme | `scss` |
| NABARD Rural Bonds | `bonds_nabard` |
| Infrastructure Bonds | `infra_bonds` |
| Stamp duty and registration fee on buying house property | `stamp_duty` |
| Interest on National Savings Certificates | `nsc_interest` |

Sub-items under 80CCC (Section 123):
| Investment Type | Limit | API key |
|----------------|-------|---------|
| Contribution to annuity plan of LIC | ₹1,50,000 | `annuity_lic` |

Sub-items under Section 124(1) / 80CCD(1):
| Investment Type | Limit | API key |
|----------------|-------|---------|
| National Pension Scheme | ₹1,50,000 | `nps` |

Sub-items under Section 124(1B) / 80CCD(1B) — Outside 80C limit:
| Investment Type | Limit | API key |
|----------------|-------|---------|
| Additional exemption on voluntary NPS | ₹50,000 | `nps_additional` |

### Section D: Section 126 Exemptions (New Code: formerly 80D)
- **Label**: "Section 126 Exemptions (Earlier: 80D Exemptions)"
- **Max limit**: ₹1,00,000.00 (per `section6a_80d_details.max_limit`)
- **Note**: "This section contains Mediclaim policies for yourself, your children, spouse and parents."

| Investment Type | Limit | API key |
|----------------|-------|---------|
| Medi Claim Policy for self, spouse, children | ₹25,000 | `mediclaim_self` |
| Medi Claim Policy for self, spouse, children (senior citizen) | ₹50,000 | `mediclaim_self_senior` |
| Medi Claim Policy for parents | ₹25,000 | `mediclaim_self_parents` |
| Medi Claim Policy for parents (senior citizen) | ₹50,000 | `mediclaim_self_parents_senior` |
| Preventive health check up | ₹5,000 | `preventive_health_checkup` |
| Preventive health check up for parents | ₹5,000 | `preventive_health_checkup_parents` |
| Medical Bills for self, spouse, children (senior citizen) | ₹50,000 | `medical_bills_self_senior` |
| Medical Bills for parents (senior citizen) | ₹50,000 | `medical_bills_parent_senior` |

### Section E: Other Investments & Exemptions
- **Label**: "Other Investments & Exemptions"
- **Note**: "Declare other investments & exemptions such as Voluntary NPS, Interest Paid on Education Loan and Medical Expenditures under this section"

Full list from API (`section_6a_items`):

| Section (New Code) | Old Code | Investment Type | Limit |
|-------------------|----------|----------------|-------|
| Section 127 | 80DD | Treatment of dependent with disability | ₹75,000 |
| Section 127 | 80DD | Treatment of dependent with severe disability | ₹1,25,000 |
| Section 128 | 80DDB | Medical expenditure for self or dependent | ₹40,000 |
| Section 128 | 80DDB | Medical expenditure for self or dependent (senior citizen) | ₹1,00,000 |
| Section 128 | 80DDB | Medical expenditure for self or dependent (very senior citizen) | ₹1,00,000 |
| Section 129 | 80E | Interest paid on Education loan | No limit |
| Section 130 | 80EE | Additional interest on housing loan (1 Apr 2016–31 Mar 2017) | ₹50,000 |
| Section 131 | 80EEA | Additional interest on housing loan (1 Apr 2019–31 Mar 2022) | ₹1,50,000 |
| Section 132 | 80EEB | Interest on electric vehicle loan (1 Apr 2019–31 Mar 2023) | ₹1,50,000 |
| Section 133 | 80G | Donation eligible for 100% exemption | No limit |
| Section 133 | 80G | Donation eligible for 50% exemption | No limit |
| Section 134 | 80GG | House rent paid (when no HRA component in salary) | ₹60,000 |
| Section 137 | 80GGC | Donation for political party | No limit |
| Section 153 | 80TTA | Interest from Savings Account | ₹10,000 |
| Section 154 | 80U | Permanent physical disability (self) | ₹75,000 |
| Section 154 | 80U | Permanent severe physical disability (self) | ₹1,25,000 |

### TCS Sections (Tax Collected at Source — also captured)
| Section | Description |
|---------|-------------|
| 206C(1) | TCS on Sale of Alcoholic liquor, forest produce, scrap, etc. |
| 206C(1C) | TCS on licensee or lessee |
| 206C(1F) | TCS on motor vehicle |
| 206C(1G) | TCS on Remittance and Tour Package |
| 206C(1H) | TCS on Domestic Goods Sales Exceeding 50 lakhs |

### New Section Numbering (Finance Act 2025 / New Income Tax Code)
Zoho has already updated labels to use the new section numbers from the Income Tax Bill 2025, while showing legacy references in parentheses:
- 80C → Section 123
- 80D → Section 126
- 80DD → Section 127
- 80DDB → Section 128
- 80E → Section 129
- 80EE → Section 130
- 80EEA → Section 131
- 80EEB → Section 132
- 80G → Section 133
- 80GG → Section 134
- 80GGC → Section 137
- 80TTA → Section 153
- 80U → Section 154
- Section 24 → Section 22
- Section 10 → Section 11
- Section 16 → Section 19
- Rebate 87A → Section 156(2)
- Relief 87AB → Section 156(2)(b)

### Header Mappings from API
```json
"headers": {
  "80c_investments": "Section 123 Investments",
  "80d_exemptions": "Section 126 Exemptions",
  "section_6a": "Deductions under Chapter VIII",
  "section_6a_legacy": "Deductions under Chapter VI-A",
  "section_10": "Section 11",
  "section_24": "Section 22",
  "section_16": "Under Section 19",
  "rebate_87a": "Rebate Under Section 156(2)",
  "relief_87ab": "Relief Under Section 156(2)(b)",
  "section_89": "Relief Under Section 157"
}
```

## Screenshots
- `screenshots/106-it-declaration-form-full.png` — Full IT Declaration form showing all sections

## Gap / Bug / Surprise
1. **Zoho already uses Income Tax Code 2025 numbering** (Section 123 for 80C, etc.). This is forward-looking and correctly prepared for the new tax code. However, the dual labeling ("Section 123 (Earlier: 80C)") could confuse users unfamiliar with the new code.
2. **80CCD(1B) NPS separate section**: Correctly isolated as it has a separate ₹50,000 limit outside the ₹1,50,000 cap.
3. **80EEB date restriction**: Correctly restricts EV loan interest to loans taken before March 2023 — aligned with the sunset clause in the Finance Act.
4. **No Aadhaar field in declaration**: PAN is captured at employee level, but Aadhaar is not part of the declaration form (correct — Aadhaar is not required for IT declaration).
5. **Missing fields**: Section 80CCD(2) (employer NPS contribution) and Section 80JJAA (employment of new employees) are not visible in the declaration form — these may be auto-calculated by Zoho.
6. **HRA sub-section not expanded**: The HRA fields (rent amount, landlord details, city type metro/non-metro) appear only when the "Is the employee staying in a rented house?" toggle is set to Yes. The actual HRA exemption formula (least of: actual HRA, 50%/40% of basic for metro/non-metro, excess rent over 10% of basic) is presumably computed by the engine.

## How We Should Build This
- Create `ITDeclaration` entity with separate sub-entities for each section
- Use new Income Tax Code 2025 section numbers as primary keys with old section numbers as display aliases
- `Section80C` entity: items list (LIC/PPF/ELSS etc.), total capped at ₹1,50,000
- Combined cap: 80C + 80CCC + 80CCD(1) share ₹1,50,000 limit
- 80CCD(1B) NPS: separate ₹50,000 additional limit
- HRA exemption: calculate as `min(actual_HRA, 50%_of_basic_for_metro_or_40%_for_non_metro, rent_paid - 10%_of_basic)` — requires work location metro/non-metro flag
- "Submit and Compare" UX: compute TDS under both regimes before the employee commits — this is excellent for employee experience
- Lock declaration after first payrun of the year (with admin override + audit log)
- Store declaration state: Draft → Submitted → Approved → Locked
