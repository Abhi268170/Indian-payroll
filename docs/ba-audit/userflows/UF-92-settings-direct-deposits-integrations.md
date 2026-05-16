# UF-92: Settings — Direct Deposits and Integrations

**Module:** Settings > Module Settings > Payments / Extensions & Developer Data > Integrations
**Tested:** 2026-05-16
**URLs:**
- Direct Deposits: `#/settings/direct-deposit`
- Zoho Apps: `#/settings/integrations/zoho`

---

## Direct Deposits Page

### URL
`#/settings/direct-deposit`

### Page Layout
- Heading: "Direct Deposits"
- "Instant Helper" button
- Two integration options: Zoho Payments Payout + Bank-specific (ICICI Bank, HSBC Bank)

---

## Option 1: Zoho Payments — Payouts

**Description:** "Pay employees directly from Zoho Payroll. Add funds to your Zoho Payments Payout Account and securely disburse salaries to multiple bank accounts without leaving the app."

**Pricing:** Platform Fee: ₹3.00 per employee per pay run (excl. 18% GST)

**GST note:** 18% GST applies → Effective cost: ₹3.54 per employee per pay run

**"How it works?" section:**
"Add funds from your verified bank account into Zoho Payments' Payout Account. On pay day, you can disburse salaries directly from Zoho Payroll to all employee bank accounts."

**Button:** "Set Up Now" → Sets up Zoho Payments Payout account

---

## Option 2: ICICI Bank Integration

**Description:** "Transfer salaries directly to your employees' bank accounts."

**Current state:** LOCKED
**Reason:** "You are currently in the Trial Plan. To configure ICICI Bank Integration, upgrade your plan."

**"Upgrade Plan" button** → Navigates to subscription/pricing page

**Status:** Requires paid plan (not available in trial)

---

## Option 3: HSBC Bank

**Description:** "Transfer salaries directly to your employees' bank accounts."

**Button:** "Set Up Now" → HSBC integration setup

**Status:** Available (no trial restriction shown) — suggests HSBC integration is included in current plan level

---

## Direct Deposit Business Context

Without Direct Deposit integration:
- Admin manually downloads Bank Advice (CSV/Excel) from pay run
- Admin uploads bank advice to employer's net banking portal
- Bank processes NEFT/RTGS/IMPS batch transfers
- Manual process: time-consuming for large payrolls

With Direct Deposit integration:
- Admin initiates transfer from within Zoho Payroll
- Zoho orchestrates bank transfer via integrated payment gateway
- Reduces manual steps; errors in file upload eliminated

**Indian banking context:**
- NEFT (National Electronic Funds Transfer): Settled in batches; operates 24x7
- RTGS (Real Time Gross Settlement): Real-time for large amounts (₹2 lakh+)
- IMPS: Instant, 24x7, up to ₹5 lakh
- Zoho Payments likely uses NEFT/IMPS for salary credits

---

## Zoho Apps Integrations Page

### URL
`#/settings/integrations/zoho`

### Available Integrations (4 confirmed)

| Integration | Description | Status |
|-------------|-------------|--------|
| Zoho People | Fetch employee and LOP details directly from Zoho People | Not connected (has "Connect" link) |
| Zoho Books | Sync all payroll transactions with Zoho Books account automatically | Not connected (has "Connect" button) |
| Zoho Expense | Helps employees submit expenses for reimbursements easily | Not connected (has "Connect" button) |
| Zoho Analytics (Beta) | Create custom reports and make better business decisions | Not connected (has "Connect" link) |

---

## Integration Details

### Zoho People Integration
- **URL:** `#/settings/integrations/zoho/people/connect/organization-sync`
- **Purpose:** Sync employee master data and LOP (Leave Without Pay) data from Zoho People
- **Benefit:** Eliminates manual LOP entry in pay runs — attendance from People auto-populates Payroll
- **Required for:** Organizations using Zoho People as their HRMS

### Zoho Books Integration
- **Purpose:** Sync payroll journal entries automatically to Zoho Books accounting
- **What syncs:** Salary expense, TDS payable, PF payable, PT payable, net pay bank entries
- **Benefit:** Auto-creates journal entries in Books after pay run is finalized
- **Required for:** Organizations using Zoho Books for accounting

### Zoho Expense Integration
- **Purpose:** Allow employees to submit reimbursement claims via Zoho Expense (not just the portal)
- **Benefit:** Zoho Expense has richer OCR bill scanning; better mobile experience
- **What syncs:** Approved expense reports from Expense → Reimbursement queue in Payroll

### Zoho Analytics Integration (Beta)
- **URL:** `#/settings/integrations/zoho/analytics`
- **Purpose:** Export payroll data to Zoho Analytics for custom dashboards and reports
- **Benefit:** Cross-module analytics beyond Zoho Payroll's built-in reports
- **Status:** Beta

---

## Business Rules
1. Direct Deposit (ICICI) requires paid plan (blocked in trial)
2. Zoho Payments Payout charges ₹3/employee/pay run + 18% GST
3. HSBC Bank integration available without trial restriction
4. All Zoho App integrations require OAuth-based "Connect" authorization
5. Zoho People integration eliminates manual LOP entry (significant for HRMS-integrated orgs)
6. Zoho Books integration auto-creates payroll journal entries (eliminates manual accounting)

## Gaps / Observations
- No integrations connected in demo org
- Zoho People connection not tested — LOP auto-sync not observed
- Zoho Books sync format not explored
- Whether HSBC integration is truly available or requires a specific plan not confirmed (no restriction message shown)

## Open Questions
- [ ] What data exactly syncs from Zoho People to Zoho Payroll (only LOP or also employee master)?
- [ ] Does Zoho Books integration handle multi-location accounting (separate cost centers per location)?
- [ ] Is Zoho Expense the preferred path for reimbursements vs the built-in employee portal?
- [ ] Can an org use multiple direct deposit methods (some employees via ICICI, others via Zoho Payments)?
