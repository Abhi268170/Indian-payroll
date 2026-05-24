# UF-73: Form 24Q — TDS Return

**Module:** Taxes & Forms > Form 24Q
**Tested:** 2026-05-16
**Mock Data Used:** Q1 FY2026-27 (April + May 2026 both TDS = ₹0)
**App State Before:** Two PAID pay runs (April and May 2026) with ₹0 TDS

## Steps Executed
1. Navigate to Taxes & Forms > Form 24Q
2. Observe quarter list and Q1 detail
3. Observe "Generate Text File" action and where it leads

---

## Form 24Q List Page

### Layout
- Table of quarters for the current financial year
- Each row represents one quarter's TDS return

### Table Columns
| Column | Value for Q1 FY2026-27 |
|--------|------------------------|
| Quarter | Q1 (April–June 2026) |
| Quarter Period | April 2026 – June 2026 |
| Due Date | 31/07/2026 |
| April TDS | ₹0.00 |
| May TDS | ₹0.00 |
| June TDS | (not yet processed — pay run date-gated) |
| Total TDS | ₹0.00 |
| Status | Not Filed / Pending |
| Action | Generate Text File |

### Quarter Configuration
| Quarter | Months | Filing Due Date |
|---------|--------|----------------|
| Q1 | April–June | 31st July |
| Q2 | July–September | 31st October |
| Q3 | October–December | 31st January |
| Q4 | January–March | 31st May |

---

## Q1 FY2026-27 Detail

### Summary Card
| Field | Value |
|-------|-------|
| Quarter | Q1 FY2026-27 |
| Period | 01/04/2026 – 30/06/2026 |
| Due Date | 31/07/2026 |
| April TDS Deducted | ₹0.00 |
| May TDS Deducted | ₹0.00 |
| June TDS Deducted | ₹0.00 (not processed) |
| Total | ₹0.00 |

### Month-wise Breakout
| Month | TDS Deducted | No. of Employees | Status |
|-------|-------------|------------------|--------|
| April 2026 | ₹0.00 | 0 | Paid / Finalized |
| May 2026 | ₹0.00 | 0 | Paid / Finalized |
| June 2026 | — | — | Not started |

---

## Generate Text File Action

Clicking "Generate Text File" navigates to a preferences/configuration URL rather than immediately generating the file.

### Pre-requisites for Generation (From Zoho Payroll Flow)
1. Tax Deductor details must be configured in Settings > Taxes:
   - TAN (Tax Deduction Account Number) — required on Form 24Q
   - Deductor Name
   - Address of Deductor
   - Responsible Person (name, PAN, designation)
2. All months in the quarter must have finalized pay runs
3. TDS challan references (BSR code, challan serial, payment date) must be uploaded for each month

### Text File Format
The generated file is a `.txt` file in the format specified by NSDL for e-filing of TDS returns (Form 24Q):
- File type: Regular / Correction statement
- Encoding: Fixed-length fields, pipe-delimited or positional
- Contains: Deductor details, Deductee (employee) details, salary breakup, TDS deducted per month, challan details
- Submitted to: TRACES/NSDL portal

---

## Form 24Q Filing Process (Full Flow)

1. **Finalize all pay runs** for the quarter — TDS liabilities locked
2. **Deposit TDS** — pay via bank against ITNS 281 challan
3. **Upload challan details** in Zoho Payroll (BSR code, date, amount, serial number)
4. **Generate Text File** from Form 24Q page
5. **Validate** file using NSDL's RPU (Return Preparation Utility) or FVU (File Validation Utility)
6. **Upload to TRACES** — receive provisional receipt and then Acknowledgment Number
7. **File status updates** in Zoho to "Filed"

---

## Statutory Reference
- Section 192: TDS on salary
- Section 200: Obligation to deduct and pay TDS
- Section 200A: Processing of TDS return
- Rule 31A: Due dates for filing TDS return
- Form 24Q: Quarterly TDS return for salary payments

---

## Cross-Module Effects
- Form 24Q data feeds into Form 16 (Part B is generated from 24Q data)
- TDS Liabilities tab tracks unpaid/paid status linked to Form 24Q months
- Employee-wise TDS data from pay runs aggregates to Form 24Q

## Gaps / Observations
- 🔴 Tax Deductor not configured in Settings > Taxes — Form 16 generation will fail; Form 24Q text file may also fail to generate correctly
- TDS = ₹0 for all months — Form 24Q is technically valid (nil return) but may be mandatory to file if registered
- "Generate Text File" navigates to preferences URL — full generation flow not tested
- TRACES upload step happens outside Zoho — Zoho only generates the text file
- No in-app validation against NSDL FVU rules visible

## Open Questions
- [ ] Can Zoho generate a nil Form 24Q (all zeroes) — is this allowed for filing?
- [ ] After generating the text file, does Zoho track whether it was actually filed on TRACES?
- [ ] What preferences are set in the "preferences" URL that "Generate Text File" redirects to?
- [ ] Is TAN (Tax Deduction Account Number) stored in the system? Where is it configured?
