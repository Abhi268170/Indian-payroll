# Settings > Module Settings > Payments > Direct Deposits

## URL
`#/settings/direct-deposit`

## Navigation Location
Settings > MODULE SETTINGS > Payments > Direct Deposits

## Purpose
Configure bank integrations to transfer salaries directly from Zoho Payroll to employees' bank accounts without manual file exports. Supports 3 payment channels.

## Page Layout
Single page. Header: "Direct Deposits" + "Instant Helper" button. Body: 3 integration cards.

---

## Integration Options

### 1. Zoho Payments - Payouts

**Description:**
> "Pay employees directly from Zoho Payroll. Add funds to your Zoho Payments Payout Account and securely disburse salaries to multiple bank accounts without leaving the app."

**Platform Fee:** ₹3.00 per employee per pay run (exclusive of 18% GST)

**How it works:**
> "Add funds from your verified bank account into Zoho Payments' Payout Account. On pay day, you can disburse salaries directly from Zoho Payroll to all employee bank accounts."

**Button:** "Set Up Now"
**Status:** Available (can be set up)

---

### 2. ICICI Bank

**Description:** "Transfer salaries directly to your employees' bank accounts."

**Status:** Locked (Trial Plan restriction)

**Lock message:**
> "You are currently in the Trial Plan. To configure ICICI Bank Integration, upgrade your plan."

**Button:** "Upgrade Plan" (not "Set Up Now")

---

### 3. HSBC Bank

**Description:** "Transfer salaries directly to your employees' bank accounts."

**Button:** "Set Up Now"
**Status:** Available (can be set up)

---

## Feature Matrix

| Integration | Available | Trial Plan | Paid Plan | Notes |
|------------|-----------|------------|-----------|-------|
| Zoho Payments Payouts | Yes | Yes | Yes | ₹3/employee/run + 18% GST |
| ICICI Bank | No (Trial) | No | Yes | Requires paid plan |
| HSBC Bank | Yes | Yes | Yes | Direct bank integration |

---

## Business Rules

1. **Zoho Payments is in-app payout** — funds are added to a Zoho-managed payout account (e-wallet/escrow); Zoho transfers to employees on pay day.
2. **ICICI Bank is direct bank API** — requires ICICI corporate banking credentials; trial plan restriction.
3. **HSBC Bank is direct bank API** — similar to ICICI but available in trial.
4. **Platform fee for Zoho Payments** — ₹3 per employee per run. For 100 employees = ₹300/month + ₹54 GST. Alternative to generating bank file and manually uploading.
5. **Manual salary transfer (no direct deposit)** — if none configured, payroll generates a bank transfer file (NEFT/RTGS format) that the employer uploads manually to their bank portal.

---

## Cross-Module Impact
| Setting | Impacts |
|---------|---------|
| Direct Deposit configured | "Pay" action in Payroll Run triggers automated transfer |
| Not configured | "Pay" action marks as paid but transfer is manual; bank file download available |

## Observations & Notes
1. **YES Bank missing** — the initial landing page text mentioned ICICI, YES Bank, and HSBC. But YES Bank is not visible in the main content — may be hidden or removed. **Need to verify.**
2. **Zoho Payments fee model** — ₹3/employee/run. For large orgs with 500+ employees, this adds up (₹1,500/month + GST). Budget consideration for clients.
3. **ICICI Bank integration** is the most common enterprise choice for Indian payroll; locking it behind paid plan is a key upsell mechanism.
4. **"Instant Helper" button** — contextual help for direct deposit setup; content not explored.
5. For our build: Generate NEFT/RTGS bank file (bank-specific format) as the default salary disbursement mechanism. Direct bank API integration is Phase 2. Bank file formats: most Indian banks use: Employee Name, Account Number, IFSC, Amount, Remarks. File format per bank varies.

## Screenshots
`docs/ba-audit/settings/screenshots/29-direct-deposits.png`
