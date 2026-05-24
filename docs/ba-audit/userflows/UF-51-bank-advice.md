# UF-51: Download Bank Advice

**Module:** Pay Runs > Pay Run Summary > Download Bank Advice
**Tested:** 2026-05-16
**Mock Data Used:** May 2026 Regular Pay Run (PAID); 2 paid employees
**App State Before:** On Pay Run Summary page (`#/payruns/3848927000000034159/summary`)

## Steps Executed
1. Observed "Download Bank Advice" button on pay run summary header
2. Button was NOT clicked (would trigger file download — not captured)
3. Documented expected format and business rules from domain knowledge

---

## Download Bank Advice Button

### Location
Pay Run Summary page — stats/header bar section, next to Payroll Cost and Total Net Pay figures.

### Button
Label: "Download Bank Advice"
Icon: Download icon (img element)
State: Enabled (pay run is PAID)

### Trigger
Clicking generates and downloads a bank advice file. File format varies by payment mode and bank integration.

---

## Bank Advice File — Expected Formats

### Format 1: Excel / CSV (Generic Bank Transfer)
Used when payment mode = "Manual Bank Transfer". Admin downloads the file and manually uploads to their bank's bulk transfer portal.

**Expected columns:**
| Column | Example Value |
|--------|--------------|
| Employee Name | Arjun Mehta |
| Employee ID | EMP001 |
| Account Number | (masked in display — from bank details) |
| IFSC Code | (from employee bank details) |
| Bank Name | (from employee bank details) |
| Account Type | Savings / Current |
| Net Pay Amount | ₹65,484.00 |
| Payment Reference | May 2026 Payroll — Arjun Mehta |
| Remarks | Regular Payroll 01/05/2026-31/05/2026 |

**May 2026 Bank Advice:**
| Employee | Net Pay |
|----------|---------|
| Arjun Mehta (EMP001) | ₹65,484.00 |
| Priya Sharma (EMP002) | ₹22,000.00 |
| **Total** | **₹87,484.00** |

### Format 2: Bank-Specific Format
Some banks (HDFC, ICICI, SBI) require a specific file format for bulk NEFT/RTGS uploads. Zoho Payroll may support:
- HDFC Bulk Transfer format
- ICICI BulkPay format
- Generic NEFT text file

### Format 3: Direct Deposit (ACH)
If Direct Deposit is configured (`#/settings/direct-deposit`), salary is sent electronically through Zoho's banking partner. Bank advice would be a confirmation file post-transfer.

---

## Business Rules
1. Bank advice includes ONLY paid employees — skipped employees are excluded
2. Net Pay amount is the same figure shown in the pay run employee summary table
3. Account numbers are used from the employee's Bank Details profile tab
4. If an employee has no bank details on file, they cannot be paid via bank transfer — they would be payment-mode "Cash" or "Cheque"
5. Bank advice download is available on PAID pay runs AND on pay runs marked as "Approved" (pre-payment)
6. The "Delete Recorded Payment" action invalidates the bank advice — it should not be used after the actual bank transfer is made

---

## Payment Mode Summary (May 2026)
| Mode | Count | Total |
|------|-------|-------|
| Manual Bank Transfer | 2 | ₹87,484.00 |
| Direct Deposit | 0 | ₹0.00 |
| Cheque | 0 | ₹0.00 |
| Cash | 0 | ₹0.00 |

---

## Gaps / Observations
- Bank advice file NOT downloaded — could not confirm exact format
- No Direct Deposit configured in demo org — direct deposit flow not tested
- Employee bank account details (account number, IFSC) not visible in bank advice (PII — may be masked)
- No bank-specific format configuration visible from pay run summary (may be in Settings > Direct Deposit)

## Open Questions
- [ ] What is the exact file format of the bank advice (Excel, CSV, or bank-specific)?
- [ ] Is there a bank-format selector before download (e.g., "HDFC format", "Generic NEFT")?
- [ ] Are account numbers included in plaintext in the download file, or masked?
- [ ] Can the bank advice be regenerated if an employee's bank details are updated after the pay run is paid?
