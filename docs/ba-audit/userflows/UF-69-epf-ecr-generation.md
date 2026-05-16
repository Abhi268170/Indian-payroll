# UF-69: EPF ECR File Generation

**Module:** Taxes & Forms > EPF / Reports > Statutory Reports > EPF ECR Report
**Tested:** 2026-05-16
**Mock Data Used:** EPF disabled for all employees; May 2026 pay run = PAID
**App State Before:** EPF Number KA/KAR/1234567/001 configured; all employee EPF = Disabled

---

## EPF ECR Overview

### What Is ECR
ECR = Electronic Challan cum Return. A monthly text file submitted to EPFO (Employees' Provident Fund Organisation) via the Unified Portal (https://unifiedportal-mem.epfindia.gov.in).

The ECR contains:
- Employer details (EPF registration number)
- Employee-wise UAN, wages, and PF contributions for the month
- EDLI and admin charges

---

## ECR Generation in Zoho Payroll

### Entry Points
1. Taxes & Forms > EPF section (if it exists — not yet fully navigated)
2. Reports > Statutory Reports > EPF ECR Report

### Pre-Requisites for ECR Generation
| Pre-requisite | Status in Demo Org |
|---------------|-------------------|
| EPF registration number configured | KA/KAR/1234567/001 ✓ |
| Employees' EPF enabled | All DISABLED — ₹0 ECR |
| UAN (Universal Account Number) per employee | Not confirmed |
| Pay run finalized for the month | May 2026 PAID ✓ |

---

## ECR File Format

EPFO specifies the ECR file format (Text file with # delimiter):

### Header Row
```
#~#EPF_REGISTRATION_NUMBER#~#MONTH#~#...
```

### Detail Rows (Per Employee)
```
UAN#~#MEMBER_NAME#~#GROSS_WAGES#~#EPF_WAGES#~#EPS_WAGES#~#EDLI_WAGES#~#EE_EPF_CONTRIBUTION#~#ER_EPF_CONTRIBUTION#~#ER_EPS_CONTRIBUTION#~#NCP_DAYS#~#REFUND_OF_ADVANCES
```

**Field Definitions:**
| Field | Description |
|-------|-------------|
| UAN | Universal Account Number (12 digits) |
| Member Name | Employee name |
| Gross Wages | Total gross salary for the month |
| EPF Wages | PF wage (capped at ₹15,000) |
| EPS Wages | Pension wage (capped at ₹15,000) |
| EDLI Wages | EDLI wage (capped at ₹15,000) |
| EE EPF Contribution | Employee PF deduction (12% of EPF wages) |
| ER EPF Contribution | Employer PF contribution (3.67% of EPF wages, above EPS) |
| ER EPS Contribution | Employer pension contribution (8.33% of EPS wages) |
| NCP Days | Non-Contributing Period days (LOP, no-pay days) |
| Refund of Advances | Amount refunded to employee from PF account |

---

## Current State — All ₹0

Since all employees have EPF Disabled:
- ECR would contain ₹0 for all contribution columns
- A nil ECR may still be required to be filed if the establishment is registered with EPFO
- EPFO dues: No challan required if ECR = ₹0

---

## EPF Calculation Reference (For When EPF Is Enabled)

**For an employee with Basic = ₹40,000:**
| Component | Formula | Amount |
|-----------|---------|--------|
| PF Wage | min(Basic, ₹15,000) = ₹15,000 (capped) | ₹15,000 |
| EE EPF | 12% × ₹15,000 | ₹1,800 |
| ER EPS | 8.33% × ₹15,000 | ₹1,250 |
| ER EPF | 3.67% × ₹15,000 | ₹550 (total ER = 12%) |
| EDLI | 0.5% × ₹15,000 | ₹75 |
| Admin Charges | 0.5% × ₹15,000 | ₹75 |

**Note:** Employer PF configuration in demo org uses "Included in Salary Structure" — meaning employer contributions are part of CTC, not an additional cost.

---

## EPF Number Mismatch (Flagged in UF-37)

EPF Number: **KA/KAR/1234567/001**
- "KA" = Karnataka state code
- "KAR" = Karnataka region
- Work location of employees: Kerala (Head Office)

**Risk:** If the EPF registration is for Karnataka but employees work in Kerala, there may be a jurisdictional issue. Typically, EPF registration is for the establishment's registered address, and employees' UAN is linked to that establishment. This may not be an error if the company's registered address is in Karnataka.

---

## ECR Submission Process

1. **Generate ECR** in Zoho (monthly, after pay run finalization)
2. **Download text file** (.txt format per EPFO spec)
3. **Log into EPFO Unified Portal** (https://unifiedportal-mem.epfindia.gov.in)
4. **Upload ECR** — portal validates file structure
5. **EPFO generates challan** with total amount due
6. **Pay challan** via net banking (by 15th of following month)
7. **Download payment confirmation** — archive for compliance

**Due Date:** 15th of the month following the contribution month
- May 2026 ECR → Challan due by 15th June 2026

---

## Taxes & Forms Navigation (Expected Sub-items)

The Taxes & Forms sidebar section likely contains:
| Sub-item | Purpose |
|----------|---------|
| TDS Liabilities | TDS deposit tracking |
| Form 24Q | Quarterly TDS return |
| Form 16 | Annual TDS certificate |
| EPF | EPF ECR and challan |
| ESI | ESI return and challan |
| Professional Tax | PT challan |
| LWF | LWF challan |

---

## Gaps / Observations
- Taxes & Forms sub-navigation not fully explored (only TDS-related pages navigated)
- EPF section URL not confirmed
- UAN field on employee profile not verified — employee may not have UAN entered
- 🔴 EPF number KA/KAR vs Kerala work location mismatch — should be clarified with Zoho support

## Open Questions
- [ ] Where is the Taxes & Forms > EPF page? What is its URL?
- [ ] Does Zoho validate UAN format before generating ECR?
- [ ] Can ECR be generated for months where EPF = ₹0 (nil return)?
- [ ] Does Zoho directly integrate with EPFO Unified Portal (API-based upload) or is it manual?
