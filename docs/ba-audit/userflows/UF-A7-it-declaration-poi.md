# UF-A7: IT Declaration Unlock and POI Approval Flow

**Module:** Employees > Investments & Proofs | Approvals > Proof Of Investments
**Tested:** 2026-05-16
**Approach:** Navigated to Arjun Mehta (EMP001) investments-and-proofs tab. Observed locked state. Clicked "Submit Declaration" to access the admin-side IT Declaration form. Also navigated to Approvals > Proof Of Investments (`#/approvals/proof-of-investment`).

---

## Findings

### 1. Employee Investments Tab Navigation

**Route:** `#/people/employees/{id}/investments-and-proofs`
**Tab label in employee profile nav:** "Investments"
**Page title:** "Investments & Proofs | Employees | Zoho Payroll"

**Sub-tabs within this page:**
| Tab | Description |
|-----|-------------|
| IT Declaration | Employee's investment declarations for tax computation |
| Proof Of Investments | Document upload and approval for submitted declarations |

**Period selector:** Dropdown showing current FY period "2026 - 27" in header.

---

### 2. IT Declaration — Locked State

**Page heading:** "IT Declaration" tab selected

**Lock message:**
> "IT Declaration submission is locked for this employee"
> "You can either allow the employee to submit IT Declaration through the portal or submit it on their behalf"

**Actions available when locked:**

| Action | Button | Route |
|--------|--------|-------|
| Submit on employee's behalf (admin) | "Submit Declaration" | `#/employees/{id}/investment-declaration/new?tax_regime=with_exemptions` |
| Allow employee to submit (portal) | No button — requires enabling employee portal access | N/A |

**Business Rule:** IT Declaration is locked by default. Two unlock paths:
1. Admin submits directly on employee's behalf (bypasses employee action).
2. Enable employee portal access → employee submits via portal (requires verified admin email).

**CSS class of lock state container:** `investment-declaration-empty-state`

---

### 3. IT Declaration Form — Admin Submission

**Route:** `#/people/employees/{id}/investment-declaration/new?tax_regime=with_exemptions`
**Query parameter:** `tax_regime=with_exemptions` — indicates old/with-exemptions regime form

**Note on tax_regime parameter:**
- `with_exemptions` = Old Regime (can claim HRA, 80C, 80D exemptions)
- For New Regime only employees, this parameter triggers the full form but exemptions would result in ₹0 benefit
- This test org uses New Regime only — the `with_exemptions` param appears to be Zoho's default CTA regardless of regime. 🟡

**Page title:** "Arjun's IT Declaration"

**Form Sections (top to bottom):**

#### Section A: House Property
| Row | Type | Description |
|-----|------|-------------|
| Is the employee staying in a rented house? | Toggle (Yes/No) | Default: No. If Yes → HRA computation sub-form opens |
| Is the employee repaying home loan for self-occupied house? | Toggle (Yes/No) | Default: No. If Yes → Sec 24(b) interest deduction input |
| Is the employee receiving rental income from let-out property? | Toggle (Yes/No) | Default: No. If Yes → rental income + loan interest offset |

#### Section B: Other Sources of Income
| Line Item | Type |
|-----------|------|
| Income from other sources | Amount input (₹) |
| Interest Earned from Savings Deposit | Amount input (₹) |
| Interest Earned from Fixed Deposit | Amount input (₹) |
| Interest Earned from National Savings Certificates | Amount input (₹) |

#### Section C: Section 123 Investments
**Section note (Zoho label):** "Section 123 Investments (Earlier: 80C Investments)"
**Note text:** "This section contains the list of investments including LIC schemes, PPF, NSC, Tuition Fees, Equity Linked Savings Schemes, etc."

**Important — Statutory Renumbering:**
The Finance Act 2025 (Budget 2025-26) renumbered Income Tax Act sections:
- Old Section 80C → New Section 123
- Old Section 80D → New Section 126
Zoho has updated its labels to reflect new section numbers with "Earlier: 80C/80D" footnote. This is correct and forward-looking. 🟢

**Input type:** Dropdown "Select an Investment" + Amount field (₹)
- User selects investment type from dropdown, then enters amount
- Multiple investments can be added ("+Add an Investment" button pattern)
- Limit: ₹1,50,000 aggregate for Section 123 (formerly 80C cap)

#### Section D: Section 126 Exemptions
**Section note (Zoho label):** "Section 126 Exemptions (Earlier: 80D Exemptions)"
**Note text:** "This section contains Mediclaim policies for yourself, your children, your parents, etc."

**Input type:** Dropdown "Select an Investment" + Amount field
- Medical insurance premium, family, parents, preventive health check-up etc.
- Limit: ₹25,000 self+family; ₹50,000 senior citizen parents

#### Section E: Other Investments & Exemptions
**Section note:** "Declare other investments & exemptions such as Voluntary NPS, Interest Paid on Education Loan, etc."

**Input type:** Dropdown "Select an Investment" + Amount field
Expected items: NPS (Section 80CCD(1B) / now renumbered), education loan interest (Section 80E / now renumbered), donations (Section 80G), etc.

#### Summary/Comparison
- **Table header:** "Particulars | Declared amount"
- This is a two-column display of all entered declarations with amounts

---

### 4. IT Declaration Form — Actions

| Button/Link | Action |
|-------------|--------|
| Add an Investment | Adds a new line item row in that section |
| Submit and Compare | Submits the declaration and shows tax comparison (new vs old regime?) |
| Cancel | Returns to `#/people/employees/{id}/investments-and-proofs` |

**"Submit and Compare" flow:**
- Submits the declaration
- Expected: shows side-by-side tax liability under Old vs New regime so employee/admin can pick optimal regime
- This is a critical feature for employees choosing between regimes
- Not tested fully (no actual data entered)

---

### 5. Approvals > Proof Of Investments

**Route:** `#/approvals/proof-of-investment`
**Also accessible via:** `#/approvals/proof-of-investment?status=pending`
**Page title:** "Approvals | POI | Zoho Payroll"

**Page heading:** "This is your space to review your employees' investment proofs!"

**Filters:**

| Filter | Type | Default | Options |
|--------|------|---------|---------|
| View | Toggle button | "All Investments" | All Investments |
| Fiscal Year | Dropdown | "2026 - 2027" | Current FY |
| Tax Regime | Dropdown | "Select Tax Regime" | New Regime, Old Regime (both) |
| Employee | Dropdown | "Select an Employee" | All employees |

**Status counter shown:** "2 employee(s) yet to submit POI"

This means 2 employees have declared investments but haven't uploaded supporting documents (POI = Proof of Investment documents).

**Table columns (expected — not rendered since no POI submissions):**
Based on domain knowledge + Zoho Payroll pattern: Employee Name | Declared Amount | POI Status (Pending / Approved / Rejected) | Action (Approve / Reject / View Documents)

**"View" button:** Present but empty state (no pending POI in this test org)

---

### 6. IT Declaration Lock/Unlock State Machine

```
Default: LOCKED
    ↓ Option 1: Admin clicks "Submit Declaration"
Admin fills IT Declaration form → clicks "Submit and Compare"
    → Declaration submitted
    → IT Declaration status: SUBMITTED (by admin)
    
Default: LOCKED  
    ↓ Option 2: Admin enables portal access for employee
Employee logs into portal → navigates to Investments
Employee fills and submits IT Declaration
    → IT Declaration status: SUBMITTED (by employee)
    
After submission:
    → Employee can upload POI documents (if portal enabled)
    → Admin reviews POI via Approvals > POI
    → Admin Approves / Rejects each document
    → Approved documents feed into TDS computation
```

---

### 7. POI Workflow (Standard Zoho Payroll Flow)

1. Admin opens POI window (via Settings → Claims and Declarations → POI dates)
2. Employees submit IT declarations (via portal) or admin submits on their behalf
3. POI window: employees upload supporting documents (investment proof PDFs/images)
4. Admin reviews each upload in Approvals > POI
5. Admin approves → TDS recomputed for the rest of the year
6. Admin rejects → employee notified, can resubmit

**POI window dates (from prior session — Settings > Claims and Declarations):**
- POI submission window has configurable start and end dates per FY
- After end date: employees cannot submit; admin can still override

---

### 8. Statutory Sections Referenced in IT Declaration

| UI Label | Old Section | New Section (Finance Act 2025) | Limit |
|----------|-------------|-------------------------------|-------|
| Section 123 Investments | 80C | 123 | ₹1,50,000 |
| Section 126 Exemptions | 80D | 126 | ₹25,000–₹50,000 |
| Voluntary NPS | 80CCD(1B) | Renumbered | ₹50,000 additional |
| Education Loan Interest | 80E | Renumbered | No limit (actual interest) |
| HRA | 10(13A) | 10(13A) | Min of 3 calculations |
| Home Loan Interest | 24(b) | Renumbered | ₹2,00,000 (self-occupied) |
| Standard Deduction | 16(ia) | 16(ia) | ₹75,000 (auto-applied) |

---

## Screenshots / Files

- `it-declaration-locked.png` — Locked IT Declaration state with "Submit Declaration" CTA
- `it-declaration-form.png` — Full IT Declaration admin form (full page screenshot)
- `approvals-poi-page.png` — POI approval page with filters and empty state

---

## Gaps / Open Questions

- [ ] **`tax_regime=with_exemptions` for New Regime employees:** Why does the admin "Submit Declaration" CTA use the `with_exemptions` param for an employee on New Regime? Does this allow old-regime declarations? Does the system enforce the employee's declared regime? 🔴
- [ ] **"Submit and Compare" output:** What does the comparison screen show? Does it compute tax under both regimes and recommend the better one?
- [ ] **Section 123 investment type dropdown:** What are all the investment options in the "Select an Investment" dropdown for each section? Not enumerated.
- [ ] **POI approval — field-level detail:** What document types are accepted (JPEG, PDF, max size)?
- [ ] **Regime switch on declaration:** Can admin/employee switch regime while filling declaration? Is there a separate form for New Regime only?
- [ ] **Previous employer investments:** Is there a field for investments declared to previous employer? Needed for employees who joined mid-year.
- [ ] **New section numbers (123/126):** Does Zoho's IT filing integration (Form 24Q/26Q) use the new renumbered sections or old section numbers? TRACES may still use old numbers. 🟡
