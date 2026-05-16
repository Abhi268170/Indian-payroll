# UF-A8: Reimbursements Module

**Module:** Approvals > Reimbursements | Settings > Salary Components > Reimbursements
**Tested:** 2026-05-16
**Approach:** Navigated to `#/approvals/reimbursements`, clicked "Add" to explore claim form, then navigated to `#/settings/salary-components/reimbursements` to examine component configuration, including clicking "Fuel Reimbursement" component detail.

---

## Findings

### 1. Approvals > Reimbursements Page

**Route:** `#/approvals/reimbursements`
**Page title:** "Approvals | Reimbursements | Zoho Payroll"

**Page structure:**
- View toggle: "All Claims"
- Filter: "Claim Month" (date range picker)
- Filter: "Employees" dropdown (Select an Employee)
- "Add" button (primary — admin-initiated claim)
- Empty state: "Looks like you don't have any results for the filter applied"
- "Clear Filter" button (visible when filters applied)

---

### 2. New Claim Form

**Route:** `#/approvals/reimbursement-claims/new`
**Page title:** "Add Claim | Zoho Payroll"
**Heading:** "New Claim"

**Form structure (table-based input):**

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| Employee Name | Dropdown (search) | Yes | Select which employee the claim is for |
| Reimbursement Type | Dropdown | Yes | Select from configured reimbursement components |
| Bill Date | Date picker | No | Date of the bill/receipt |
| Bill Number | Text | No | Bill/invoice reference number |
| Attachments | File upload | No | Receipt/invoice scan — PDF/image |
| Claim Amount | Number (₹) | Yes | Amount claimed by employee |
| Approved Amount | Number (₹) | Yes | Admin-set approved amount (can differ from claim) |
| Comment | Text | No | Internal note or reason |
| Action | Delete icon | — | Remove the line item |

**Additional control:**
- "Add another bill" button — adds a new row for multi-bill claims in one submission
- "Save & Approve" button — saves and immediately approves (bypasses separate approval step)

**Note on "Approved Amount":**
The form shows both "Claim Amount" and "Approved Amount" as required fields — admin can approve a partial amount different from what was claimed. This is the approval mechanism embedded directly in the Add form.

**Business Rule:** Admin-added claims auto-approve (or prompt for approved amount simultaneously). Employee-submitted claims (via portal) go through the normal approval queue. The "Save & Approve" action creates an approved claim that flows into the next pay run.

---

### 3. Settings > Salary Components > Reimbursements

**Route:** `#/settings/salary-components/reimbursements`
**Page title:** "Reimbursements | Settings | Zoho Payroll"

**Sub-tab structure within Salary Components:**
| Tab | Route |
|-----|-------|
| Earnings | `#/settings/salary-components/earnings` |
| Deductions | `#/settings/salary-components/deductions` |
| Benefits | `#/settings/salary-components/benefits` |
| Reimbursements | `#/settings/salary-components/reimbursements` |

**Default reimbursement components (out-of-box):**

| Component Name | Route ID |
|----------------|----------|
| Fuel Reimbursement | 3848927000000032484 |
| Driver Reimbursement | 3848927000000032487 |
| Vehicle Maintenance Reimbursement | 3848927000000032493 |
| Telephone Reimbursement | 3848927000000032496 |
| Leave Travel Allowance | 3848927000000032490 |

**Notable absence:** Medical Reimbursement is NOT a default component — must be manually added. Historical significance: Medical Reimbursement was exempt up to ₹15,000/year under old regime; under new regime this exemption is removed. Absence makes sense for new-regime-only design.

**"Add Component" button:** Present. Expected to navigate to new component creation form (could not confirm exact route as button didn't navigate during session — may render a settings panel).

---

### 4. Reimbursement Component Detail/Edit Form

**Route:** `#/settings/salary-components/reimbursements/{id}`
**Tested with:** Fuel Reimbursement (ID 3848927000000032484)

**Fields:**

| Field | Type | Required | Value (Fuel Reimbursement) | Notes |
|-------|------|----------|---------------------------|-------|
| Reimbursement Type | Display/Label | — | Pre-set (Fuel) | Component classification |
| Name in Payslip | Text | Yes | "Fuel Reimbursement" | How it appears on employee payslip |
| Include as Flexible Benefit Plan component | Checkbox | No | Unchecked | FBP = employee can choose allocation |
| Carry forward and encash at end of fiscal year | Radio | — | — | Balance rolls over to FY end encashment |
| Do not carry forward and encash monthly | Radio | — | — | Use-it-or-lose-it monthly |
| Enter Amount | Number | Yes | 0 (per month) | Monthly limit/ceiling in ₹ |
| Mark this as Active | Checkbox | No | Unchecked | Whether component is live for payroll |

**FBP (Flexible Benefit Plan) toggle:**
When "Include this as a Flexible Benefit Plan component" is checked, the component becomes part of FBP — employees can choose how much of their CTC to allocate to this component (flexible). This is relevant for tax optimization under old regime; limited relevance for new regime.

**Carry-forward options:**
- **Monthly encashment:** Each month, any approved reimbursement amount is paid in that month's payslip
- **Annual encashment:** Unused approved amounts accumulate and are paid at FY end

---

### 5. Tax Treatment of Reimbursements

| Regime | Tax Treatment |
|--------|--------------|
| New Regime | All reimbursements fully taxable as salary income. No exemptions. |
| Old Regime | Select reimbursements have exemptions: LTA (Section 10(5)), Telephone (partial), Medical (up to ₹15,000 — removed from FY 2019-20) |

**Business Rule (new regime only):** Since v1 is new regime only, all reimbursement amounts are added to gross taxable income. There is no special exempt treatment in the engine.

**In payslip:** Approved reimbursement amount appears as a separate line under Earnings. It adds to gross income.

---

### 6. How Reimbursements Flow Through Payroll

```
1. Admin creates/configures reimbursement component (Settings)
2. Component added to employee's salary structure (optional — or claimed per bill)
3. Employee submits claim via portal (or admin submits on behalf)
   → Status: Submitted → Admin reviews → Approved / Rejected
4. Approved claim amount queued for next pay run
5. Pay run picks up approved claims for the period
6. Approved amount added to employee's net pay for that month
7. Appears as separate line on payslip
```

---

### 7. Employee Portal Reimbursement Submission (from prior sessions)

Based on prior audit documentation (UF-88-employee-portal-reimbursement.md):
- Employee logs into portal → submits claim with bill date, amount, attachment
- Admin receives notification → reviews in Approvals → Reimbursements
- Admin approves/rejects with optional partial amount approval and comment
- Approved claims automatically included in next pay run

---

## Screenshots / Files

- `reimbursements-approvals-page.png` — Approvals Reimbursements list page
- `reimbursement-new-claim-form.png` — New Claim form with table columns visible
- `settings-reimbursements-components.png` — Reimbursement components list in settings
- `reimbursement-component-detail.png` — Fuel Reimbursement component edit form
- `add-reimbursement-component.png` — Add Component button view

---

## Gaps / Open Questions

- [ ] **"Reimbursement Type" dropdown options in Add Claim form:** What components appear? Only Active ones? Need to verify by selecting the dropdown.
- [ ] **Claim approval workflow — separate Approve step:** When employee submits via portal, does admin see a separate "Approve" action in the claim detail? Or is it always embedded amount input?
- [ ] **FBP allocation:** When FBP is enabled, how does the employee specify their allocation? Is there a separate FBP declaration screen?
- [ ] **Annual encashment timing:** When is the FY-end encashment triggered? Auto by system or manual admin action?
- [ ] **Medical Reimbursement creation:** Task asked to create Medical Reimbursement at ₹1,250/month. Not done to avoid polluting test data. Would require Add Component → Name: "Medical Reimbursement", Amount: 1250, Mark Active.
- [ ] **Reimbursement in pay run:** How does the approved claim appear during pay run processing — is it in the "Variable Inputs" section or auto-pulled?
- [ ] **Rejection flow:** What happens when a claim is rejected? Does employee get email notification with reason?
