# UF-95: Settings — Tax Details (TAN / Organisation Tax Configuration)

**Module:** Settings > Organisation Settings > Taxes > Tax Details
**Tested:** 2026-05-16
**URL:** `#/settings/taxes`

---

## Navigation

Settings > Organisation Settings > Taxes (expand) > Tax Details

---

## Page Layout

- Heading: "Tax Details"
- "Instant Helper" button
- Form with two sections:
  1. Organisation Tax Details
  2. Tax Deductor Details

---

## Section 1: Organisation Tax Details

| Field | Type | Required | Current Value | Validation |
|-------|------|----------|---------------|------------|
| PAN | Text | Yes (*) | ABCDE1234F | Format: AAAAA0000A |
| TAN | Text | No | MUMR12345A | Format: AAAA00000A |
| TDS circle / AO code | 4-part text | No | Blank | Format: AAA / AA / 000 / 00 |
| Tax Payment Frequency | Display (disabled) | N/A | Monthly | Not editable |

### PAN (Permanent Account Number)
- Organisation's PAN (not individual employee's PAN)
- Required for TDS returns (Form 24Q)
- Format: AAAAA0000A (5 alpha + 4 digit + 1 alpha)
- 4th character indicates PAN type: "A" = Company, "B" = BOI, "P" = Individual, etc.
- Demo value: ABCDE1234F (test PAN — invalid in production)
- Placeholder: `AAAAA0000A`

### TAN (Tax Deduction Account Number)
- Issued by Income Tax Department to entities deducting TDS
- Required for: TDS deposit, Form 24Q filing, Form 16 issuance
- Format: AAAA00000A (4 alpha + 5 digit + 1 alpha)
- First 4 chars: Jurisdiction code (e.g., "MUMR" = Mumbai Range)
- Demo value: MUMR12345A (TAN now configured — previously observed as missing)
- **Important:** Prior sessions flagged "Tax Deductor not found" for Form 16 — TAN is now filled

### TDS Circle / AO Code
4-part composite field:
| Part | Placeholder | Format | Meaning |
|------|-------------|--------|---------|
| Part 1 | AAA | 3 alpha | AO Number prefix |
| Part 2 | AA | 2 alpha | Ward prefix |
| Part 3 | 000 | 3 digits | Range |
| Part 4 | 00 | 2 digits | Ward |

AO Code (Assessing Officer Code) is required for:
- Income Tax audit enquiries
- Jurisdictional determination
- Form 24Q field

### Tax Payment Frequency
- Display only (not editable): "Monthly"
- Indicates TDS is deposited monthly (not quarterly)
- Statutory: TDS for all months except March → due 7th of following month; March → due 30th April

---

## Section 2: Tax Deductor Details

### Deductor's Type (Radio)
- Employee (checked — default)
- Non-Employee

**"Employee" type:** The responsible person for TDS is an employee of the organization (e.g., Finance Manager).

**"Non-Employee" type:** The responsible person is a non-employee (e.g., CA / external consultant who manages TDS).

### Deductor's Name
- Combobox: "Select a Tax Deductor"
- Populated from the Users list (org users)
- The selected person's PAN is linked to Form 24Q as the responsible person for TDS
- Currently: "Select a Tax Deductor" (not configured)

### Deductor's Father's Name
- Text field (disabled until deductor is selected)
- Auto-populated from user profile once deductor is selected

---

## Save Button

"Save" button at bottom of form.
Validates required fields (PAN) before saving.

---

## Statutory Significance of Tax Details

| Field | Used In |
|-------|---------|
| Organisation PAN | Form 24Q header, Form 16 Part A |
| TAN | TDS challan (ITNS 281), Form 24Q, Form 16 |
| AO Code | Form 24Q, tax jurisdiction |
| Deductor's Name | Form 24Q, Form 16 (signing authority) |
| Tax Payment Frequency | Determines TDS deposit schedule |

**Without TAN:** Form 24Q cannot be generated, Form 16 cannot be issued. TDS challan cannot be recorded.

**Without Deductor:** Form 24Q signature field is empty — return may be rejected by TRACES.

---

## Prior Observations Updated

**Prior session:** "Tax Deductor not found" error on Form 16 (UF-74).
**Current observation:** TAN field shows "MUMR12345A" — TAN IS configured.

**Reconciliation:** The Form 16 error "Tax Deductor not found" likely refers specifically to the **Deductor's Name** field in Section 2 (the responsible person), not the TAN. Selecting a deductor in the combobox should resolve the Form 16 prerequisite error.

---

## Business Rules
1. Organisation PAN is mandatory for TDS returns and Form 16
2. TAN is mandatory for TDS deposit, Form 24Q, Form 16 — now configured
3. AO Code required for Form 24Q — currently blank (may not block generation but needed for complete return)
4. Deductor's Name = responsible person for TDS deduction — required for Form 24Q signing
5. Tax Payment Frequency = Monthly (hardcoded for this org size/type — cannot change in UI)

## Gaps / Observations
- AO Code is blank — Form 24Q may generate with incomplete data
- Deductor's Name not selected — "Tax Deductor not found" error likely persists on Form 16
- TAN "MUMR12345A" is now configured — resolves TAN-related blocks but Deductor Name still missing
- 🔴 To fully unblock Form 16 and Form 24Q: Select a Deductor from the dropdown

## Open Questions
- [ ] Is AO Code mandatory for Form 24Q or can it be filed without it?
- [ ] Will selecting the admin user (abhijithss2255) as Deductor resolve the Form 16 error?
- [ ] Does Zoho validate the TAN format (AAAA00000A) on save?
- [ ] Can there be multiple TAN numbers for multi-state establishments?
