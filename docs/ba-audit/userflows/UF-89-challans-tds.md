# UF-89: TDS Challans

**Module:** Taxes & Forms > Challans
**Tested:** 2026-05-16
**URL:** `#/taxes-and-forms/tax-payments/unassociated`
**Mock Data Used:** Demo org; all TDS = ₹0
**App State Before:** TDS Liabilities all ₹0

---

## Navigation

Taxes & Forms sidebar (expanded) → "Challans"

**Taxes & Forms Sub-navigation (complete list confirmed):**
| Sub-item | URL |
|----------|-----|
| TDS Liabilities | `#/taxes-and-forms/tax-liabilities/pending` |
| Challans | `#/taxes-and-forms/tax-payments/unassociated` |
| Form 24Q | `#/taxes-and-forms/form24q` |
| Form 16 | `#/taxes-and-forms/form16` |

**Note:** EPF, ESI, PT, LWF are NOT sub-items of Taxes & Forms in the sidebar. These are accessible only via Reports > Statutory Reports.

---

## Challans Page

### Tabs
| Tab | URL | Description |
|-----|-----|-------------|
| Unassociated | `/unassociated` | Challans paid but not yet linked to TDS liabilities |
| Associated | `/associated` | Challans linked to specific TDS liability periods |

### Current State
"You have no unassociated Challans as of now."

Empty state text: "Your Challans will be displayed here if you have payment amount that hasn't been associated to TDS liabilities. You can record a new Challan and associate it's payment amount to TDS liabilities."

---

## What Is a TDS Challan

TDS is deposited to the Income Tax Department via challan **ITNS 281** through the banking system (authorized banks or NSDL e-payment portal).

**Challan flow:**
1. Admin computes TDS liability for the month (from TDS Liabilities page — UF-68)
2. Admin pays TDS challan via:
   - Bank (net banking) at authorized bank using ITNS 281
   - NSDL e-payment portal (https://onlineservices.tin.egov-nsdl.com)
3. Admin receives a **Challan Identification Number (CIN)** / BSR Code + Challan Serial Number
4. Admin records this challan in Zoho

### Recording a Challan in Zoho
Click "New" button → Opens challan recording form

**Expected Challan Form Fields:**
| Field | Type | Required | Notes |
|-------|------|----------|-------|
| TAN | Display (auto-fill) | Yes | From Tax Details settings |
| Challan Type | Dropdown | Yes | ITNS 281 (TDS) |
| BSR Code | Text | Yes | 7-digit bank branch code |
| Challan Date | Date | Yes | Date of payment |
| Challan Serial Number | Text | Yes | From bank receipt |
| Amount Deposited | Currency (₹) | Yes | Total TDS paid |
| Nature of Payment | Dropdown | Yes | TDS on Salary (Section 192) |
| Month/Quarter | Dropdown | Yes | Month for which TDS is being paid |

---

## Challan Association

After recording a challan:
1. Challan appears in "Unassociated" tab
2. Admin clicks "Associate" on the challan
3. Links challan to specific TDS liability period (e.g., May 2026 TDS)
4. Challan moves to "Associated" tab
5. TDS Liability for May 2026 is marked "Paid"

**Why association matters:**
- Ensures specific challan is linked to specific month's liability
- Required for accurate Form 24Q filing (each quarter's TDS must be backed by challans)
- TRACES reconciliation matches challan CIN with TDS return data

---

## "Instant Helper" Button

Present on Challans page. Likely opens a help modal with step-by-step guidance on recording TDS challans.

---

## Business Rules
1. TDS must be deposited by 7th of the following month (March: 30th April)
2. One challan can cover TDS for one month only (generally)
3. BSR Code (Bank Branch) + Challan Serial Number + Date = unique CIN identifier
4. Challan must be associated with specific liability before Form 24Q can be filed
5. Late deposit attracts interest: 1.5% per month u/s 201(1A)

## Gaps / Observations
- Challan recording form not tested (no TDS liability to pay — all ₹0)
- TAN not configured — challan recording may be blocked
- Challan association flow not directly observed

## Open Questions
- [ ] Can admin record a challan without TAN configured?
- [ ] What happens if challan amount > TDS liability for the month (surplus)?
- [ ] Can one challan be associated with multiple months' liabilities?
- [ ] Does Zoho auto-validate the BSR Code format (7 digits)?
