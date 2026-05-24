# Employees > Add Employee — Step 4: Payment Information (Bank Details)

## URL / Navigation Path
- Route: `#/people/employees/{id}/edit/payment-details`
- Full URL: `https://payroll.zoho.in/#/people/employees/3848927000000032948/edit/payment-details`
- Entry: Reached after saving Personal Details (Step 3) in the wizard
- Page title: "Employees | Payment Information | Employees | Edit | Zoho Payroll"

## Purpose
Step 4 of 4 in Add Employee wizard. Captures payment method and bank account details for salary disbursement.

## Payment Methods
Four payment mode options rendered as custom `.ind-payment-mode-option` divs (not standard radio buttons). Selecting one shows its relevant sub-form.

| Mode | Description | Notes |
|---|---|---|
| Direct Deposit (Automated) | Bank transfer via automated file (NEFT/RTGS batch) | Requires full bank details |
| Bank Transfer (Manual) | Manual bank transfer | Requires bank details; payroll generates a transfer advice |
| Cheque | Physical cheque payment | Minimal fields (likely just name) |
| Cash | Cash payment | No bank details required |

**Default selected:** Direct Deposit (Automated)

## Bank Details Sub-Form (Direct Deposit / Bank Transfer)

| Field | Type | Required | Format / Validation | Notes |
|---|---|---|---|---|
| Account Holder Name | Text | Yes | Free text | Pre-populated with employee name; editable |
| Bank Name | Text | Yes (auto-filled) | Free text | Auto-populated from IFSC lookup; editable if lookup fails |
| Account Number | Password (masked) | Yes | Numeric; shown as dots | type=password — stored masked; "Show A/C No" reveals full number |
| Re-enter Account Number | Password (masked) | Yes | Must match Account Number | Confirmation field; prevents typos |
| IFSC Code | Text | Yes | Format: `AAAA0000000` (4 alpha + 7 alphanumeric); placeholder shows format | Live IFSC validation on entry; populates Bank Name + Branch on success |
| Account Type | Radio | Yes | Current / Savings | Default: Savings |

### IFSC Auto-Validation Behaviour
On entering a valid IFSC and tabbing out:
- Shows "Verified" status indicator
- Populates Bank Name: e.g., "HDFC Bank"
- Populates Branch: e.g., "PARK STREET"
- If invalid IFSC: shows error; Bank Name field remains empty

### Account Number Masking
- Entered as password field (dots visible during entry)
- After save: displayed as `XXXX{last 4 digits}` (e.g., `XXXX6789`)
- Profile view has "Show A/C No" link — clicking reveals full number (role-permissioned)
- Applies PCI-DSS / data privacy best practice for financial data

## Buttons & Actions

| Button | Behaviour |
|---|---|
| Save and Continue | Validates required fields; saves and navigates to Summary/Confirmation |
| Skip | Skips payment details; navigates to Summary without bank details |

## EMP001 Data Filled
- Payment Mode: Bank Transfer (Manual)
- Account Holder Name: Arjun Mehta
- Bank Name: HDFC Bank (auto-filled from IFSC)
- Account Number: 50100123456789
- IFSC: HDFC0001234 → Verified: "HDFC Bank, PARK STREET"
- Account Type: Savings

## Key Observations for Our Build
1. **IFSC live lookup** — our payment API must integrate with an IFSC directory (e.g., Razorpay IFSC API or static DB) to auto-populate bank name and branch. Failure must degrade gracefully (manual entry).
2. **Account number as password field** — store encrypted (AES-256) per CLAUDE.md security rules. Never store or log in plaintext.
3. **Display masking pattern** — always show `XXXX` + last 4 digits in UI. Full reveal = authorised role + audit log entry.
4. **Four payment modes** — our PaymentMethod enum must have: `DirectDeposit`, `BankTransfer`, `Cheque`, `Cash`. Bank details fields only required for first two.
5. **Re-enter confirmation** — frontend validation only; our API should also validate account number consistency.
6. **Skip is valid** — payment details are optional at onboarding. Payroll run must gate disbursement on bank details being present.
7. **Account Type matters** — Savings vs Current affects NACH/NEFT mandate type for bulk disbursement.

## Cross-Module Impact
- Bank details feed into payroll disbursement (Bank Transfer File / NEFT advice).
- Account number masking applies across all views: payslip, employee profile, bank transfer report.
- If payment mode = Cash or Cheque: no bank file generated for this employee in payroll run.

## Screenshots
- `screenshots/38-payment-information.png` — Payment mode selection and bank details form
- `screenshots/38-bank-details-filled.png` — Completed bank details with IFSC verified for EMP001
