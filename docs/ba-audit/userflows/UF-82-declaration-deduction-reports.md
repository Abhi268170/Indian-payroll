# UF-82: Declarations & Deduction Reports

**Module:** Reports > Declarations & Investments + Deduction Reports
**Tested:** 2026-05-16
**Mock Data Used:** Demo org; IT declarations status from UF-26; no investments declared by employees
**App State Before:** Reports Centre; individual pages not opened

## Steps Executed
1. Identified 3 declaration reports and 4 deduction reports from Reports Centre
2. Cross-referenced with TDS/declaration data from UF-26 to UF-35
3. Documented expected content

---

## Declarations & Investments Category (3 Reports)

### Report 1: FBP Declaration Report

**Purpose:** Flexible Benefit Plan allocation per employee for the financial year. Shows how employees have allocated their FBP component across eligible sub-components.

**FBP (Flexible Benefit Plan) Context:**
FBP is a salary structuring tool where a component (e.g., "Flexi Allowance") allows the employee to allocate the amount across multiple eligible sub-components (Food, LTA, Books & Periodicals, etc.) that carry tax exemptions.

**Expected Columns:**
| Column | Notes |
|--------|-------|
| Employee | |
| FBP Component | Total FBP allocation |
| Food Allowance | ₹ (exempt up to ₹26,400/year — ₹2,200/month) |
| LTA | ₹ (exempt per Section 10(5)) |
| Books & Periodicals | ₹ (exempt) |
| Uniform Allowance | ₹ (exempt) |
| Other Sub-components | ₹ each |
| Total Allocated | Should = FBP component amount |
| Status | Declared / Not Declared |

**Current Data:** FBP declarations not observed in demo org — Arjun and Priya have Fixed Allowance (fully taxable) not an FBP component.

---

### Report 2: Investment Declaration Report

**Purpose:** All IT investment declarations per employee for the financial year. Used by finance team to verify TDS computations.

**Expected Columns:**
| Column | Arjun (Expected) |
|--------|-----------------|
| Employee | Arjun Mehta |
| PAN | ABCP01234A |
| Tax Regime | New Regime |
| HRA Exemption Claimed | ₹0 (new regime: HRA not exempt) |
| 80C — LIC / ELSS / PPF | ₹0 (new regime: 80C not applicable) |
| 80D — Medical Insurance | ₹0 |
| 80E — Education Loan | ₹0 |
| 80G — Donations | ₹0 |
| 80TTA — Savings Interest | ₹0 |
| Other Deductions | ₹0 |
| NPS (80CCD(1B)) | Permitted in new regime (₹50,000) |
| Total Deductions Declared | ₹0 (locked declaration) |
| Status | Locked (Arjun) / Open (Priya) |
| Submission Date | — |

**Note:** In new regime, most deductions (80C, HRA, LTA) are not claimable. New regime declarations are simpler — primarily NPS (80CCD(1B)) and employer NPS contribution (80CCD(2)).

---

### Report 3: Proof of Investment Report

**Purpose:** POI document submission status per employee per financial year. Admin uses this to track who has submitted proof and who has not.

**Expected Columns:**
| Column | Notes |
|--------|-------|
| Employee | |
| Investment Type | 80C (LIC), 80D (Mediclaim), etc. |
| Declared Amount | What employee declared |
| Proof Submitted | Yes / No |
| Proof Verified | Yes / No / Rejected |
| Verified Amount | Admin-verified figure (may differ from declared) |
| Proof Submission Deadline | (configured in Settings) |
| Status | Pending / Verified / Rejected |

**Current Data:** No POI submissions observed in demo org.

---

## Deduction Reports Category (4 Reports)

### Report 4: Benefits & Deductions Summary

**Purpose:** Combined view of all earnings (benefits) and all deductions per employee per month.

**Expected Structure:**
| Section | Components |
|---------|------------|
| Earnings | Basic, HRA, All Allowances, Variable Pay, Reimbursements |
| Deductions | EPF (EE), ESI (EE), PT, TDS, Loan EMI, Other deductions |
| Net Pay | Earnings − Deductions |

---

### Report 5: Deductions Summary

**Purpose:** All deductions (statutory + non-statutory) per employee.

**Expected Columns:**
| Column | Arjun (May 2026) |
|--------|-----------------|
| Employee | Arjun Mehta |
| Month | May 2026 |
| EPF (EE) | ₹0.00 |
| ESI (EE) | ₹0.00 |
| PT | ₹0.00 |
| TDS | ₹0.00 |
| Loan EMI | ₹0.00 (starts July 2026) |
| Other | ₹0.00 |
| Total Deductions | ₹0.00 |
| Net Pay | ₹65,484.00 |

---

### Report 6: Benefits Summary

**Purpose:** All benefit components paid per employee (non-deduction side). Useful for verifying allowances.

**Expected Columns:**
| Column | Arjun (May 2026) |
|--------|-----------------|
| Employee | Arjun Mehta |
| Month | May 2026 |
| Basic | ₹37,417 (prorated 29/31) |
| HRA | ₹14,967 |
| Fixed Allowance | ₹13,100 |
| Variable Pay | ₹0 |
| Reimbursements | ₹0 |
| Total Earnings | ₹65,484 |

---

### Report 7: Donations Summary

**Purpose:** Section 80G donation claims per employee. Shows donated amounts, recipient organizations, and eligibility for deduction.

**New Regime Note:** Section 80G deductions are NOT available under the new tax regime. In new regime, all 80G claims result in ₹0 tax benefit. However, the declaration may still be collected for completeness.

**Expected Columns:**
| Column | Notes |
|--------|-------|
| Employee | |
| Recipient Organization | Name of NGO/trust |
| 80G Qualification | 100% / 50% deduction eligible |
| Donated Amount | ₹ |
| Eligible Deduction | ₹ (after qualification percentage) |
| Tax Regime | If New Regime: deduction = ₹0 benefit |
| Proof Submitted | Yes / No |

---

## Gaps / Observations
- Individual declaration/deduction report pages not opened
- 🟡 FBP Report: no FBP component in demo org salary structures — report will be empty
- 🟡 Donations Summary: in new regime, all 80G deductions are disallowed — report may still show declarations for reference
- Investment Declaration Report: Arjun's declaration is LOCKED and shows no declared amounts — zero figures across the board
- No TDS impact of any declarations currently visible (all TDS = ₹0)

## Open Questions
- [ ] Investment Declaration Report: Is there a comparison view (Declared vs Proof Verified vs Allowed)?
- [ ] FBP Report: Available only if salary structure contains an FBP component?
- [ ] Donations Summary: Does the report flag new-regime employees attempting 80G claims?
- [ ] POI Proof deadline — where is this configured? Per financial year or per pay run cycle?
