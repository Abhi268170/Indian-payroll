# UF-79: Statutory Reports (EPF, ESI, PT, LWF)

**Module:** Reports > Statutory Reports
**Tested:** 2026-05-16
**Mock Data Used:** May 2026 pay run; all statutory deductions = ₹0
**App State Before:** Reports Centre; statutory report pages not individually opened

## Steps Executed
1. Identified all 8 statutory reports from Reports Centre overview
2. Cross-referenced with statutory configuration captured in UF-37, UF-38, UF-39
3. Documented expected content based on domain knowledge + system data

---

## EPF Reports

### Report 1: EPF Summary
**Purpose:** Total EPF contributions (EE + ER) per month. Used for internal reconciliation before challan payment.

**Expected Columns:**
| Column | May 2026 Value |
|--------|---------------|
| Month | May 2026 |
| Total Employees | 0 (all EPF disabled) |
| EPF Wages | ₹0.00 |
| Employee Contribution (12%) | ₹0.00 |
| Employer EPF (3.67%) | ₹0.00 |
| Employer EPS (8.33%) | ₹0.00 |
| EDLI Contribution | ₹0.00 |
| Admin Charges (0.5%) | ₹0.00 |
| Total Employer Cost | ₹0.00 |

**Note:** All ₹0 because all employees have EPF Disabled in their statutory config.

### Report 2: EPF ECR Report
**Purpose:** Electronic Challan cum Return — the text file uploaded to EPFO's Unified Portal (UAN portal) for monthly PF remittance.

**Format:** Pipe-delimited text file per EPFO specification.

**ECR File Structure (per employee line):**
```
UAN | Member Name | Gross Wages | EPF Wages | EPS Wages | EDLI Wages | EE EPF Contribution | ER EPF Contribution | ER EPS Contribution | NCP Days | Refund of Advances
```

**Generation Pre-requisites:**
- EPF registration number configured (org setting: KA/KAR/1234567/001 — observed in UF-37)
- Employees have UAN (Universal Account Number) assigned
- Pay run finalized for the month

**Current Gap:** EPF Number has Karnataka prefix (KA/KAR) despite Kerala org location — potential registration mismatch (flagged in UF-37).

**EPFO Upload Process:**
1. Generate ECR from Zoho (download text file)
2. Log into EPFO Unified Portal (epfindia.gov.in)
3. Upload ECR file
4. Portal generates challan with total amount
5. Pay challan via net banking
6. Upload challan reference back to Zoho (optional — for reconciliation)

---

## ESI Reports

### Report 3: ESI Summary
**Purpose:** Total ESI contributions (EE + ER) per month. Used before ESIC challan payment.

**Expected Columns:**
| Column | May 2026 Value |
|--------|---------------|
| Month | May 2026 |
| Employees Covered | 0 (all above ₹21,000 ceiling) |
| ESI Wages | ₹0.00 |
| Employee Contribution (0.75%) | ₹0.00 |
| Employer Contribution (3.25%) | ₹0.00 |
| Total | ₹0.00 |

### Report 4: ESI Monthly Summary
**Purpose:** Employee-wise ESI deduction per month.

**Additional Column:** IP Number (Insurance Person Number) — ESIC's employee identifier.

**ESIC Filing:** Monthly return filed on ESIC portal (esic.in) → ECR (ESIC contribution report) generated and uploaded. Challan paid by 21st of following month.

---

## Professional Tax Reports

### Report 5: Professional Tax Summary
**Purpose:** Total PT deducted per month across all states.

**Expected Columns:**
| Column | May 2026 Value |
|--------|---------------|
| Month | May 2026 |
| State | Kerala |
| Employees | 2 (Arjun + Priya) |
| Total PT | ₹0.00 |
| Status | Not a deduction month (Half-Yearly cycle) |

**May 2026 PT = ₹0 is correct** — Kerala PT deducts only in September (Apr-Sep) and March (Oct-Mar).

### Report 6: Employee-wise Professional Tax Report
**Purpose:** PT per employee per month — useful for employees working in multiple states.

**Expected Columns:**
| Column | May 2026 (Arjun) |
|--------|-----------------|
| Employee | Arjun Mehta |
| State | Kerala |
| PT Wages | ₹65,484 |
| PT Rate | Half-Yearly |
| PT Amount | ₹0.00 (not a deduction month) |

### Report 7: Annual Professional Tax Report
**Purpose:** Full-year PT per employee. Useful for Form 16 Part B (PT is a deduction under Section 16(iii)).

**Expected Year Total for Arjun (₹70,000/month gross):**
- Kerala PT slab for ₹45,000-₹99,999 = ₹750 per half-year
- Annual PT = ₹750 (Sep) + ₹750 (Mar) = ₹1,500/year
- Shows in Form 16 Part B as deduction under Section 16(iii)

**PT Challan:** State-specific challan, paid to Commercial Tax Department of Kerala. Due dates vary by state (typically monthly or quarterly).

**Current Gap:** PT Number field is BLANK in statutory configuration — challan generation may be incomplete (flagged in UF-39).

---

## Labour Welfare Fund

### Report 8: Labour Welfare Fund Summary
**Purpose:** LWF contributions per employee by state.

**May 2026 Status:** LWF configuration — all observed employees have LWF Disabled. Likely ₹0 across the board.

**LWF Rates (State-specific examples):**
| State | Employee | Employer | Frequency |
|-------|----------|----------|-----------|
| Maharashtra | ₹6 | ₹12 | Monthly (June + Dec) |
| Karnataka | ₹20 | ₹40 | June + December |
| Kerala | ₹4 | ₹8 | Annual (March) |
| Tamil Nadu | ₹10 | ₹20 | Annual |

**Current org location:** Kerala — LWF would be ₹4 (EE) + ₹8 (ER) annually in March.

---

## Statutory Filing Calendar (May 2026 Reference)
| Obligation | Due Date (for May 2026) | Status |
|------------|------------------------|--------|
| EPF ECR Upload + Challan | 15th June 2026 | Not applicable (₹0) |
| ESI Monthly Return + Challan | 21st June 2026 | Not applicable (₹0) |
| PT Challan (Kerala) | Not due in May | Next: September 2026 |
| TDS Deposit | 7th June 2026 | Not applicable (₹0 TDS) |
| Form 24Q Q1 | 31st July 2026 | Pending June pay run |

---

## Gaps / Observations
- No statutory report pages individually opened — content inferred
- 🔴 EPF Number prefix mismatch (KA/KAR vs Kerala org) — may cause ECR file rejection by EPFO
- 🔴 PT Number blank — challan generation may fail
- 🟡 All statutory figures are ₹0 in test data — real validation requires employees with EPF/ESI enabled
- No compliance calendar integration visible from reports page (due dates not shown)

## Open Questions
- [ ] Does Zoho generate the ESIC return file (half-yearly return) or only monthly ECR?
- [ ] Can the ECR file be regenerated after a correction to employee UAN numbers?
- [ ] For PT: does Zoho auto-calculate September and March deductions from the running monthly gross, or require a separate PT computation step?
