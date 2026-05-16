# UF-27: IT Declaration — Employee Declaration Form (Admin-Submitted)

**Module:** Employees → Employee Profile → Investments → IT Declaration → Submit Declaration
**Tested:** 2026-05-16
**Mock Data Used:** Arjun Mehta EMP001, FY 2026-27

## Steps Executed
1. Navigated to Arjun Mehta → Investments tab → IT Declaration sub-tab
2. Observed locked state with "Submit Declaration" button
3. Clicked "Submit Declaration" → navigated to declaration form
4. Observed "Arjun's IT Declaration" form
5. Documented visible sections and fields
6. Captured screenshot

## IT Declaration Form — Page Structure

**Page title:** "Arjun's IT Declaration"
**Info banner:** "Enter your planned investment declarations here and choose the desired regime in the following page."
- Indicates this is a **multi-step form**: declarations on this page, regime selection on next page

**Two-column layout:** Particulars (left) | Declared Amount (right)

## Sections Visible (top of form)

### House Property Section (top, unlabeled section header)

Three toggle questions:

| Particular | Type | Default |
|------------|------|---------|
| Is the employee staying in a rented house? | Toggle (Yes/No) | No |
| Is the employee repaying home loan for a self occupied house property? | Toggle (Yes/No) | No |
| Is the employee receiving rental income from let out property? | Toggle (Yes/No) | No |

**Behavior:** When toggled to "Yes", additional input fields presumably expand (not captured — toggles were all "No").

### Other Sources of Income Section

| Particular | Type | Notes |
|------------|------|-------|
| Other Sources of Income | Accordion / Expandable row | Right-arrow (>) to expand |

### Section 123 Investments (Earlier: 80C Investments)

**Section label note:** "(Earlier: 80C Investments)" — This label references old tax regime terminology. Section 80C deductions do NOT apply under the new tax regime. The UI's use of "Section 123 Investments" appears to be a renaming for new regime terminology, though the parenthetical "(Earlier: 80C Investments)" may cause confusion.

**Section note:** "This section contains the list of investments including LIC schemes, mutual funds and PPF. The maximum limit for this section is ₹1,50,000.00"

| Field | Type | Notes |
|-------|------|-------|
| Select an Investment | Dropdown | Lists investment types (LIC, PPF, ELSS, etc.) |
| Amount | Number (₹) | Adjacent to dropdown; default 0 |

**Layout pattern:** Dropdown + Amount field on same row; likely allows adding multiple rows.

## Multi-step Form — Page Navigation

The info banner mentions "choose the desired regime in the following page" — confirming at minimum 2 pages:
1. **Page 1:** Investment declarations (this page)
2. **Page 2:** Tax regime selection

This aligns with the Settings config "Allow employees to switch tax regimes" — if enabled, employees see regime choice after entering declarations.

## Business Rules

1. **HRA declaration** — "Rented house" toggle triggers HRA-related inputs. For new regime, HRA exemption under Section 10(13A) is NOT available (new regime does not allow most exemptions). System may still collect this for old-regime employees or future use.

2. **Section 80C under new regime** — Standard 80C deductions (PPF, ELSS, LIC, etc.) are NOT deductible under new tax regime (FY2024-25 onwards). The UI's "Section 123 Investments (Earlier: 80C Investments)" is ambiguous — needs clarification on whether this affects TDS computation for new regime employees.

3. **₹1,50,000 investment limit** — Refers to old-regime Section 80C aggregate limit. Under new regime, this limit is irrelevant. UI displaying this limit may mislead employees.

4. **Fiscal year filter** — The Investments tab had a "Period: 2026-27" dropdown filter. Declarations are FY-scoped.

5. **Admin-on-behalf submission** — Admin can submit the declaration on behalf of the employee (bypassing portal lock). The URL was observed to carry `?tax_regime=with_exemptions` parameter when admin submits — suggesting the system pre-selects "with exemptions" (old regime) mode for admin submissions. This may be incorrect for new-regime-only organizations.

## 🔴 Critical Compliance Concern

The `?tax_regime=with_exemptions` URL parameter observed when admin submits IT declaration on behalf suggests the declaration is being defaulted to old regime / with-exemptions mode. In a new-regime-only v1 product, this creates a TDS computation error risk — employees' TDS may be computed under old regime when admin submits on their behalf.

## Data Relationships
- IT Declaration → Employee (M:1)
- IT Declaration → Fiscal Year (M:1)
- IT Declaration → Tax Regime (old/new — per declaration)
- IT Declaration → Investment Line Items (1:N, one per investment category)
- IT Declaration → House Property entries (1:N)
- IT Declaration → POI (Proof of Investment) (1:N, created during POI window)

## State Machine

```
[Not Started / Locked] 
    → Admin clicks "Submit Declaration" → [Form Open — Admin Filling]
    → Employee submits via portal → [Form Open — Employee Filling]
[Form Open]
    → Submit → [Submitted — Pending POI]
[Submitted]
    → POI window opens → Employee uploads proofs → [POI Submitted]
    → Admin approves → [Approved]
    → TDS recomputed on approved declaration
```

## Navigation
- Entry: Employee profile → Investments tab → IT Declaration sub-tab → "Submit Declaration" button
- URL: `#/employees/{id}/investments/it-declaration?tax_regime=with_exemptions` (observed)
- Page 2 (regime selection): next step after filling declarations

## Screenshots
- [IT Declaration form — top section](../screenshots/UF-27-IT-declaration-form.png)

## Complete Section Inventory (Updated from Full Form Exploration)

### Table structure: 2 columns — Particulars | Declared Amount

**Section 1: House Property Toggles (3 items)**
- Is the employee staying in a rented house? (Toggle No/Yes)
- Is the employee repaying home loan for a self occupied house property? (Toggle No/Yes)
- Is the employee receiving rental income from let out property? (Toggle No/Yes)

**Section 2: Other Sources of Income (Accordion — expands to 4 rows)**
| Row | Type |
|-----|------|
| Income from other sources | Number ₹ |
| Interest Earned from Savings Deposit | Number ₹ |
| Interest Earned from Fixed Deposit | Number ₹ |
| Interest Earned from National Savings Certificates | Number ₹ |

**Section 3: Section 123 Investments (Earlier: 80C Investments)**
- Note: "List of investments including LIC schemes, mutual funds and PPF. Maximum limit ₹1,50,000.00"
- Repeatable rows: [Investment type dropdown] + [Amount ₹]
- "Add an Investment" button

**Section 4: Section 126 Exemptions (Earlier: 80D Exemptions)**
- Note: "Mediclaim policies for yourself, children, spouse and parents. Maximum limit ₹1,00,000.00"
- Repeatable rows: [Investment type dropdown] + [Amount ₹]
- "Add an Investment" button

**Section 5: Other Investments & Exemptions**
- Note: "Voluntary NPS, Interest Paid on Education Loan and Medical Expenditures"
- Repeatable rows: [Investment type dropdown] + [Amount ₹]
- "Add an Investment" button

**Form action buttons:**
- "Submit and Compare" — submits to regime comparison page (step 2)
- "Cancel" link — returns to Investments tab

## Gaps / Observations
- 🔴 `?tax_regime=with_exemptions` parameter in admin-submit URL — forces old regime mode; incorrect for new-regime-only orgs
- 🔴 Section 123 displays ₹1,50,000 limit and Section 126 displays ₹1,00,000 limit — these OLD regime limits are NOT applicable under new tax regime. Displaying them misleads employees.
- No "Previous Employment Details" section in IT Declaration form — prior employer YTD entered separately via import (see UF-25)
- Regime selection page (step 2) not captured — "Submit and Compare" not clicked
- HRA declaration fields (when toggle = Yes) not explored
- Investment type dropdown options not enumerated (not clicked open)
- No "Save as Draft" button — unclear if partial saves are possible before "Submit and Compare"
- Section naming (123, 126) vs old section numbers (80C, 80D) creates confusion — parenthetical "(Earlier: 80X)" helps but old limits still shown
