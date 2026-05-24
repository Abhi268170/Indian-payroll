# UF-88: Employee Portal — Reimbursement Submission

**Module:** Employee Portal > Reimbursements
**Tested:** 2026-05-16
**Mock Data Used:** Demo org; all reimbursement components INACTIVE
**App State Before:** Reimbursement components all INACTIVE; zero max amounts

---

## CONFIGURATION-GATED FLOW

All reimbursement components in the demo org are INACTIVE with ₹0 maximum:
- Fuel & Travel Reimbursement: INACTIVE
- Driver Reimbursement: INACTIVE
- Vehicle Maintenance Reimbursement: INACTIVE
- Telephone Reimbursement: INACTIVE
- Leave Travel Allowance: INACTIVE

**Employee portal reimbursement section may be hidden or show empty state when all components are inactive.**

**To test this flow:** Activate at least one reimbursement component and set a maximum amount in Settings > Salary Components.

---

## Employee Portal — Reimbursements Section (Expected)

### Navigation (Employee View)
Employee Portal App → Home → Reimbursements
OR: Employee Portal App → Bottom nav → Reimbursements

### Reimbursements List View
| Status | Description |
|--------|-------------|
| Pending | Submitted, awaiting admin approval |
| Approved | Admin approved; will be paid in selected payout month |
| Rejected | Admin rejected; can be re-submitted |
| Paid | Reimbursement included in a finalized pay run |

---

## Claim Submission Flow (Employee)

### Step 1: Tap "Submit Claim" or "+"
Opens claim submission form.

### Step 2: Claim Form Fields
| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Reimbursement Type | Dropdown | Yes | From active components: Fuel, Telephone, LTA, etc. |
| Claim Amount (₹) | Currency | Yes | Amount being claimed |
| Claim Date | Date | Yes | Date of expense |
| Description | Text | Optional | Purpose of expense |
| Bill/Receipt Upload | File | Yes (for most types) | PDF, JPG; proof of expense |

### Step 3: Submit
- Claim sent to admin approval queue
- Employee sees status: "Pending"

### Step 4: Admin Reviews (Approvals Module)
- Admin sees claim in Approvals > Reimbursements
- Admin can approve full amount, approve partial amount, or reject

### Step 5: Payout Month Selection
On approval, admin selects payout month (e.g., June 2026)
- Claim included in June 2026 pay run
- Employee receives reimbursement in June salary

---

## Claim Amount Limits

| Limit | Behavior |
|-------|---------|
| Component maximum | Configured in Settings; claim cannot exceed max |
| ₹0 maximum (current demo) | Claim submission may be blocked or show warning |
| Annual limit (LTA) | 2 journeys per 4-year block |

---

## Bill Upload Requirements

| Requirement | Detail |
|-------------|--------|
| Format | PDF, JPG, PNG |
| Max size | Typically 5MB per file |
| Multiple bills | Yes — for multi-expense claims |
| OCR/AI extraction | Unknown — Zoho may auto-read bill details |

---

## Reimbursement Taxability (Employee Perspective)

| Type | Employee Sees | Admin Notes |
|------|--------------|-------------|
| Fuel (with bills) | Tax-free | Must match actual expense |
| Telephone (official use) | Tax-free | Official use documentation needed |
| LTA | Tax-exempt (conditions) | Economy fare; actual travel required |
| Amount > actual expense | Taxable perquisite | System may not auto-flag this |

**In New Tax Regime:** Most reimbursement exemptions not available — employee receives reimbursement as income. Employer should still reimburse but advise employee of taxability.

---

## Post-Approval Employee View

After claim is approved:
- Status changes to "Approved"
- Payout month shown: "Will be paid in June 2026"
- Once pay run finalized: Status = "Paid"
- Reimbursement appears on June 2026 payslip

After claim is rejected:
- Status: "Rejected"
- Rejection reason shown (if admin provided)
- Employee can submit a new claim (not edit and resubmit the same one)

---

## Business Rules
1. Reimbursement must have admin approval before payout
2. Max amount per component set by admin in Settings (currently ₹0 — blocking)
3. Bill upload is required for expense verification
4. Admin can approve partial amount (less than claimed)
5. Payout is in a specific pay run month (not immediate)
6. Rejected claims require fresh submission — no "edit and resubmit"
7. LTA claims: 2 per 4-year block (FY1, FY2, FY3, FY4 — any 2 years)

## Gaps / Observations
- Employee portal Reimbursements section not navigated (all components INACTIVE)
- File upload UI and OCR capability not tested
- LTA block-year tracking mechanism not explored
- 🟡 To complete this flow: Activate "Fuel & Travel Reimbursement", set max ₹5,000; test full cycle

## Open Questions
- [ ] What does the employee see in the Reimbursements section when all components are inactive?
- [ ] Is there an edit option for a pending (not yet reviewed) claim?
- [ ] Can employee recall/cancel a submitted claim before admin reviews it?
- [ ] Does Zoho validate that the claim date is within the current financial year?
- [ ] For LTA: Does Zoho track the 4-year block and warn when limit reached?
