# Pay Runs > Payslip Full View & TDS Sheet

## URL / Navigation Path

**Payslip (split panel within summary page):**
`https://payroll.zoho.in/#/payruns/{id}/summary` — split panel slides in from right on employee row click or "View" click

**TDS Sheet PDF iframe:**
API endpoint: `GET /api/v1/employees/{employeeId}/taxworksheet?month={YYYY-MM}&accept=pdf&print=true`
Rendered inside an `<iframe>` within a modal overlay on the summary page.

No dedicated full-page URL for payslip was found. The payslip is always rendered within the split panel or as a modal PDF — not a standalone page with its own hash route.

## Purpose

Provides read-only payslip view for completed pay runs (finalised payslip in Approved/Paid states) and the TDS computation worksheet. These are the primary artifacts for employee distribution and income tax filing.

## Payslip Split Panel — Full Field Inventory

### Header Section
| Field | Type | Notes |
|-------|------|-------|
| Company Name | Read-only | Organisation name from Settings |
| Company Logo | Image | If uploaded in Settings |
| Pay Period | Read-only | e.g., "May 2026" |
| Payslip title | Read-only | "Payslip" |

### Employee Details Section
| Field | Type | Notes |
|-------|------|-------|
| Employee Name | Read-only | Full name |
| Employee ID | Read-only | System-assigned (e.g., EMP001) |
| Designation | Read-only | From employee profile |
| Department | Read-only | From employee profile |
| Date of Joining | Read-only | From employee profile |
| PAN | Read-only | Masked: XXXXX1234X |
| UAN | Read-only | PF Universal Account Number (if PF configured) |
| Bank Account | Read-only | Masked account number |
| IFSC | Read-only | Bank branch code |
| Payment Mode | Read-only | e.g., "Manual Bank Transfer" |

### Attendance Section
| Field | Type | Notes |
|-------|------|-------|
| Total Working Days | Read-only | Base days in month (e.g., 31) |
| Paid Days | Read-only | Base Days − LOP Days |
| LOP Days | Read-only | As entered |

### Earnings Section
| Field | Type | Notes |
|-------|------|-------|
| Basic | Read-only ₹ | Prorated if LOP |
| HRA | Read-only ₹ | Prorated if LOP |
| Fixed Allowance | Read-only ₹ | Prorated if LOP |
| Bonus (if any) | Read-only ₹ | One-time earning |
| Commission (if any) | Read-only ₹ | One-time earning |
| Leave Encashment (if any) | Read-only ₹ | One-time earning |
| Reimbursements (if any) | Read-only ₹ | Non-taxable |
| **Gross Earnings** | Read-only ₹ | **Sum of all earnings** |

### Deductions Section
| Field | Type | Notes |
|-------|------|-------|
| TDS (Income Tax) | Read-only ₹ | Computed or overridden |
| Professional Tax | Read-only ₹ | State-specific |
| PF Employee | Read-only ₹ | 12% of PF wage (if configured) |
| ESI Employee | Read-only ₹ | 0.75% of ESI wage (if configured) |
| Other Deductions | Read-only ₹ | Ad-hoc deductions |
| **Total Deductions** | Read-only ₹ | **Sum of all deductions** |

### Net Pay Section
| Field | Type | Notes |
|-------|------|-------|
| **Net Pay** | Read-only ₹ | **Gross − Total Deductions** |
| Net Pay in words | Read-only | "Rupees Eighty-Seven Thousand..." |

## TDS Sheet (Tax Computation Worksheet)

### Access

- "View" button in TDS Sheet column in employee table
- "View TDS Sheet" from row kebab menu
- Available in Draft, Approved, Paid states

### Rendering

Rendered as a server-generated PDF inside an HTML `<iframe>` within a modal. DOM inspection not possible. Content captured via screenshot only.

### API Endpoint (confirmed from network inspection)

```
GET /api/v1/employees/{employeeId}/taxworksheet
  ?month={YYYY-MM}
  &accept=pdf
  &print=true
```

### Content Structure (observed from screenshot)

**Section 1: Employee Information**
- Employee Name, PAN, Designation, Department

**Section 2: Annual Projected Income**
- Basic × 12 (projected)
- HRA × 12
- Fixed Allowance × 12
- Other components × 12
- Total Projected Gross

**Section 3: Deductions (New Regime)**
- Standard Deduction: ₹75,000 (FY2026 — new regime)
- Note: Under new regime, only Standard Deduction applies; 80C/80D/HRA exemptions are NOT available

**Section 4: Taxable Income**
- Total Projected Gross − Standard Deduction

**Section 5: Tax Computation (New Regime Slabs — FY2026)**
| Slab | Rate |
|------|------|
| ₹0 – ₹4,00,000 | 0% |
| ₹4,00,001 – ₹8,00,000 | 5% |
| ₹8,00,001 – ₹12,00,000 | 10% |
| ₹12,00,001 – ₹16,00,000 | 15% |
| ₹16,00,001 – ₹20,00,000 | 20% |
| ₹20,00,001 – ₹24,00,000 | 25% |
| ₹24,00,001 and above | 30% |

**Section 6: Rebate**
- Rebate u/s 87A: ₹25,000 rebate if taxable income ≤ ₹7,00,000 (new regime)

**Section 7: Surcharge**
- 10% if income 50L–1Cr
- 15% if income 1Cr–2Cr
- 25% if income 2Cr–5Cr
- 37% if income >5Cr
- (Marginal relief applies)

**Section 8: Health & Education Cess**
- 4% on (tax + surcharge)

**Section 9: Monthly TDS Distribution**
- Total Annual Tax Liability
- Tax paid in prior months (YTD)
- Remaining months in financial year
- TDS per month for remaining months

## Statutory Notes

- **New Regime only (FY2026)** — Zoho Payroll is configured for new regime in this org. TDS Sheet shows no 80C/80D/HRA deductions.
- **§206AA** — if employee has not submitted PAN, TDS must be at higher of applicable rate or 20% (flat). The system should flag employees without valid PAN.
- **Prior employer YTD** — for mid-year joiners, prior employer TDS and income must be factored into the TDS computation. The TDS Sheet should show prior employer income/tax in Section 9. Not tested.

## Key Observations for Our Build

1. **Payslip has no dedicated URL** — it's always embedded in the summary page. Our build should provide both: embedded split panel view AND a standalone payslip URL (e.g., `/payslip/{runId}/{employeeId}`) for email links and bookmarking.
2. **TDS Sheet is PDF-only** — our build should expose TDS computation as structured JSON via API (`GET /api/payroll-runs/{runId}/employees/{employeeId}/tds-worksheet`) AND generate a PDF from that structured data. This makes the data testable.
3. **New regime standard deduction ₹75,000** — not hardcoded; must come from statutory config table. FY2025 was ₹50,000 (old regime) / ₹75,000 (new regime). Store in `StatutoryConfig.TaxSlabs` keyed by FY + regime.
4. **Payslip in words** — "Net Pay in words" field is expected on Indian payslips (compliance convention). Implement a `NumberToWordsIndian` utility in the engine. Handles lakh/crore system (not million/billion).
5. **PAN masking** — XXXXX1234X on payslip. Full PAN stored encrypted (AES-256). Only authorised roles + audit log can view unmasked PAN.
6. **Form 16 relationship** — the TDS Sheet computation per month rolls up into Form 16 (annual TDS certificate). Store monthly TDS computation inputs/outputs as an immutable audit record per payroll run, per employee.

## Screenshots

- `screenshots/56-payslip-view-post-approval.png` — Read-only payslip split panel (post-approval)
- `screenshots/56-tds-sheet-modal.png` — TDS Sheet PDF iframe modal
- `screenshots/62-download-payslip-dialog.png` — Download Payslip dialog with password protection

---
*Audit session: May 2026 | Pay Run ID: 3848927000000034159*
