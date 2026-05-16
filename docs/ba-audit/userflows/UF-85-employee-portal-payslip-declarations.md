# UF-85: Employee Portal — Payslip, Declarations, and POI

**Module:** Employee Self-Service Portal
**Tested:** 2026-05-16
**Mock Data Used:** Portal settings observed via admin
**App State Before:** IT Declaration is locked org-wide; portal access enabled

## Steps Executed
1. Employee portal mobile app links identified from Dashboard
2. Settings > Employee Portal > Preferences captured
3. IT Declaration settings captured (`#/settings/preferences/it-declaration`)
4. Portal web URL not directly navigated (separate employee login required)

---

## Employee Portal Access Mechanism

### Mobile App
- iOS: Apple App Store — "Employee Portal - Zoho Payroll" (App ID: 1450810850)
- Android: Google Play — `com.zoho.payroll`
- Employee downloads app, logs in with their Zoho account credentials

### Web Portal (Expected)
- Likely accessible at `payroll.zoho.in` with employee (non-admin) Zoho account
- Employee is invited via admin → receives invite email → creates Zoho account → logs in
- Exact web URL not confirmed from admin console

### Invitation Flow (Expected)
1. Admin adds employee to system
2. Admin sends invitation (via "Send Invitation" button on employee profile — not investigated)
3. Employee receives email with activation link
4. Employee sets up Zoho account
5. Employee can access portal (web or mobile)

---

## Employee Portal Features (What Employees Can Do)

### Available After Portal Access Enabled

| Feature | Admin Action Required | Employee Action |
|---------|----------------------|-----------------|
| View payslips | None (auto-available) | Download PDF |
| View TDS worksheet | None | View/download |
| IT Declaration submission | Admin must release declaration | Submit via portal |
| FBP Declaration | Admin must configure FBP component | Allocate components |
| POI upload | IT Declaration must be released | Upload documents |
| Reimbursement claim | None | Submit claim with amount and type |
| View Form 16 | Admin must publish Form 16 | Download PDF |
| View documents | Admin must enable "Show documents" | View/download |
| Query/contact HR | Always (contact email configured) | Email HR |

---

## IT Declaration Release Flow

**Current State:** IT Declaration is LOCKED.

**To release:**
1. Admin navigates to `#/settings/preferences/it-declaration`
2. Clicks "Release IT Declaration"
3. All employees can now submit declarations via portal
4. Admin receives notifications as employees submit

**After release:**
- Employees access portal → "IT Declaration" section
- Declare investments (HRA rent paid, 80C amounts, 80D, NPS, etc.)
- Choose tax regime (new vs old — if "Allow employees to switch tax regimes" is enabled → YES, it is enabled)
- Submit declaration
- Admin reviews in Approvals > POI section (if POI upload required)
- TDS computation updates to reflect declarations

---

## Tax Regime Switching (Key Business Rule)

From `#/settings/preferences/it-declaration`:
- **"Allow employees to switch tax regimes" = CHECKED**
- This means employees can change between new and old tax regime via the portal
- System-wide setting — applies to all employees
- Relevant for FY2025-26 and FY2026-27

This is significant for the SaaS being built: despite the build plan being "new regime only in V1", Zoho Payroll as the reference product supports regime switching. Any employee who has not opted for new regime can switch to old regime.

**Implication for TDS:** When an employee switches regime, the entire TDS projection changes:
- New regime: Fewer exemptions, flat standard deduction ₹75,000, no HRA/LTA/80C
- Old regime: HRA exemption, LTA, full 80C/80D, multiple deductions

---

## POI Submission Flow (Expected)

**Pre-condition:** IT Declaration must be released AND employee must have submitted IT Declaration with investment amounts declared.

1. Employee declares investment amounts in IT Declaration (e.g., ₹1,50,000 under 80C)
2. At year-end (typically Jan-Mar), admin sets POI submission window
3. Employees upload proof documents for each declared investment:
   - 80C: LIC premium receipts, PPF passbook, ELSS mutual fund statements
   - 80D: Health insurance premium receipts
   - HRA: Rent receipts + landlord PAN (if rent > ₹1 lakh/year)
4. Admin reviews in Approvals > Proof of Investments
5. Admin approves each document
6. TDS for remaining months adjusts based on verified amounts

---

## FBP Declaration (Expected)

**Pre-condition:** An FBP component must be configured in Salary Components > Reimbursements.

Current state: "No Active FBP component" — FBP feature is inactive in demo org.

When FBP is configured:
1. Employee sees "FBP Declaration" in portal
2. Allocates their FBP pool across sub-components (Food, LTA, Books, etc.)
3. Declaration determines which sub-components are active for the year
4. FBP claims (with bills) are submitted monthly and approved by admin

---

## Reimbursement Claim Submission (Expected)

1. Employee logs into portal
2. Navigates to Reimbursements section
3. Clicks "New Claim"
4. Selects Reimbursement Type (Medical, Travel, etc.)
5. Enters amount
6. Attaches supporting document (receipt, bill)
7. Selects Claim Month
8. Submits → appears in Approvals > Reimbursements for admin review

---

## Gaps / Observations
- Employee portal not directly logged into — all content from admin settings perspective
- 🟡 IT Declaration LOCKED — prevents testing actual declaration submission flow
- Web portal URL not found — only mobile app confirmed
- No invitation flow observed (employee invite via email not tested)
- FBP not configured — FBP declaration flow not testable
- "Allow TDS modification to exceed calculated tax amount" = NOT checked — employees cannot overpay TDS (conservative setting)

## Open Questions
- [ ] What is the employee web portal URL? Is it `payroll.zoho.in/app#/...` with different role?
- [ ] How does an employee receive their invite? Is there a "Send Invite" button on employee profile?
- [ ] Can employees update personal details (address, bank) via portal?
- [ ] Is there a mobile app for admin (not just employees)?
- [ ] When employee submits IT Declaration, does admin get an email notification?
