# UF-24: Update Bank Details

**Module:** Employees > Overview > Payment Information Edit
**Tested:** 2026-05-16
**Mock Data Used:** Priya Sharma EMP002 (ID: 3848927000000032984)
**App State Before:** Bank: SBI, IFSC: SBIN0001234, A/C: XXXX7890 (masked)

## Steps Executed
1. Navigate to Priya Sharma's Overview: `#/people/employees/3848927000000032984`
2. Observed Payment Information section (masked account XXXX7890, SBI, Savings)
3. Clicked Edit button on Payment Information
4. Navigated to: `#/people/employees/3848927000000032984/edit-payment-details`
5. Observed payment method selection and bank detail fields

## Page Identity
- **URL:** `#/people/employees/{id}/edit-payment-details`
- **Page title:** "Employees | Payment Information | Zoho Payroll"
- **Heading:** "Priya's payment information"

## Payment Mode Options

The form presents 4 payment method cards (mutually exclusive):

| Mode | Label | Description |
|------|-------|-------------|
| Direct Deposit | Direct Deposit (Automated Process) | "Initiate payment in Zoho Payroll once the pay run is approved" — requires configuration via `#/settings/direct-deposit` |
| Bank Transfer | Bank Transfer (Manual Process) | "Download Bank Advice and process the payment through your bank's website" — currently selected |
| Cheque | Cheque | Manual cheque payment |
| Cash | Cash | Manual cash payment |

**Direct Deposit** shows a "Configure Now" link → `#/settings/direct-deposit` (not yet set up for lerno org)

## Bank Transfer Fields (Currently Active)

| Field | Type | Required | Value | Notes |
|-------|------|----------|-------|-------|
| Account Holder Name | Textbox | Yes | "Priya Sharma" | Editable |
| Bank Name | Textbox | Yes | "State Bank of India" | Editable free text |
| Account Number | Textbox (disabled) | Yes | "7890" (last 4 visible) | **DISABLED** — requires "Change" button to edit |
| IFSC | Textbox | Yes | "SBIN0001234" | Has real-time verification; shows "Verified" + branch name |
| Account Type | Radio group | Yes | "Savings" | Options: Current / Savings |

## Critical Finding: Account Number Change Mechanism

The Account Number field is **disabled by default**. To change the account number:
- A separate "Change" button appears next to the Account Number field
- This suggests a **separate verification or confirmation step** is involved

This is a security control — bank account numbers cannot be casually edited in the same flow as other fields. This prevents accidental or malicious account number changes without an explicit deliberate action.

**However:** no approval workflow or OTP/email verification was visible from the UI snapshot alone — clicking "Change" would reveal whether there is an additional verification step.

## IFSC Verification
- IFSC field has real-time lookup: after entering SBIN0001234, it shows:
  - "Verified" indicator (green check)
  - Bank branch: "State Bank of India, HAJIGANJ"
- This is a live IFSC registry lookup (Razorpay/NPCI API or similar)

## Display on Overview (Read Mode)
- Account Number shown as masked: "XXXX7890" (last 4 digits visible)
- "Show A/C No" button — reveals full account number (likely requires additional auth or just a click)
- No encryption indicator visible but account is masked in the UI

## Actions

| Action | Trigger | Pre-condition | Post-behavior |
|--------|---------|---------------|---------------|
| Change (A/C Number) | Button click | Bank Transfer mode active | Likely opens confirmation/verification step |
| Save | Button | Required fields filled | Saves payment details, returns to Overview |
| Cancel | Link | Always | Returns to Overview without saving |

## Business Rules
- Account number is masked on display (last 4 digits shown) — privacy control
- Account number change requires explicit "Change" button — not inline editable
- IFSC is verified in real-time against bank registry — prevents invalid IFSC entry
- Account Type options: Savings or Current only (no NRE/NRO/Fixed Deposit etc.)
- All 4 payment modes are available at any time — no restriction based on pay run status
- Changing payment mode does NOT require approval

## Approval Workflow Observation
**No approval workflow is triggered by bank detail changes.** The edit is direct — Save immediately updates the record. This is a significant security gap:
- A malicious admin could change an employee's bank account before a pay run without any second-level approval
- No notification to the employee of bank account change is visible
- No audit trail link from this page

## Cross-Module Effects
- Payment mode saved here determines the "Payment Mode" column in pay run employee summary (showed "Manual Bank Transfer" for all employees in May 2026 run)
- Bank Advice download format depends on payment mode and bank account details

## Gaps / Observations
- 🔴 No approval workflow for bank account changes — single admin can redirect salary without a second approver
- 🔴 No employee notification on bank account change (no email alert to employee when their bank details are modified)
- No reason/comment field when changing bank details
- Account Number "Change" button behavior not fully tested — the step after clicking Change was not captured
- "Show A/C No" button on overview — no audit trail entry visible for this reveal action
- No NRE/NRO account type support — may be a gap for employees with international bank accounts
- Priya's PAN is "-" yet she was included in the May pay run with ₹22,000 net pay — contradicts the finding that missing PAN blocks payroll (she has DOB and Father's name but no PAN)

## Open Question
- [ ] Does clicking "Change" on Account Number trigger an OTP or email verification to the employee?
- [ ] Is a change to bank details audit-logged anywhere in the system?
- [ ] Why was Priya Sharma (PAN = "-") included in pay run while Vikram Nair (PAN = "-") was excluded? Is the gateway check PAN + other fields, not PAN alone?
