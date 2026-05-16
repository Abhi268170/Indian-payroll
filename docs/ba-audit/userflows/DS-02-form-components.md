# DS-02: Design System — Form Components and Validation Patterns

**Module:** Design System / Form Patterns
**Observed from:** Create Employee, Create Loan, Salary Structure Assignment, Pay Run initiation, Settings forms
**Tested:** 2026-05-16

---

## Form Component Inventory

### Text Input (Single Line)
**Usage:** Employee name, loan name, org profile name, reason fields
**Attributes:**
- Label above or to the left
- Placeholder text in grey
- Required indicator: asterisk (*) on label
- Active state: blue border
- Error state: red border + error message below
- Character limit: varies by field

### Number / Currency Input (Spinbutton)
**Usage:** Loan Amount (₹ spinbutton), LOP Days, Instalment Amount
**Attributes:**
- Currency prefix "₹" displayed inside field (not as label)
- Supports decimal input (observed: 2 decimal places for ₹ amounts)
- Spinner arrows (up/down) on right side
- Validation: min value (0), max value (varies)
- Integer-only where applicable (LOP days)

**Critical business rule:** All monetary fields use ₹ Indian Rupee symbol. Decimal precision = 2 decimal places (paisa precision). No field observed using float — consistent with "decimal only" architecture rule.

### Date Picker
**Usage:** DOB, DOJ, Loan Disbursement Date, Pay Date, Declaration Deadline
**Format:** `dd/MM/yyyy` — Indian date format (not ISO 8601)
**Attributes:**
- Calendar popup on click
- Manual entry supported (validated against format)
- Future date restriction: varies by field
- Past date restriction: varies by field
- Error: "Invalid date" if format wrong

### Dropdown (Select)
**Usage:** State, Gender, Department, Pay Schedule, Calculation Type
**Types:**
1. Simple select — non-searchable, fixed options
2. Combobox — searchable with free text (for Employee search, Loan Name from configured list)
3. Multi-select — not commonly observed

**Indian-specific dropdowns observed:**
- State: All 28 states + 8 UTs of India
- Gender: Male / Female / Other (or similar)
- Tax Regime: New Regime / Old Regime

### Toggle Switch
**Usage:** Enable Portal Access (Employee Portal), Mark component Active/Inactive, statutory settings
**States:** Active (colored) / Inactive (grey)
**Label:** Appears next to the toggle

### Checkbox
**Usage:** Exempt from perquisite (Loan), Allow employees to switch tax regimes, Show documents in portal
**States:** Checked / Unchecked / Indeterminate (for select-all in tables)
**Label:** Appears to the right of the checkbox

### Radio Button
**Usage:** Tax Regime selection (New vs Old), Pay Frequency selection
**States:** Selected / Unselected
**Group:** Mutually exclusive within a group

### File Upload
**Usage:** Documents module (employee document upload), POI proof upload
**Patterns:**
1. Drag-and-drop zone with "Choose File" fallback button
2. File type restriction: `.pdf` specified
3. Size limit: 50MB per file
4. ZIP support: When uploading as ZIP, files inside must match employee IDs (automated assignment)

### Text Area (Multi-line)
**Usage:** Reason (Loan creation), Banner Message (Employee Portal)
**Attributes:**
- Resizable
- Character limit (if any) not always visible
- No rich text formatting observed

### Searchable Combobox
**Usage:** Employee search in pay run, Employee filter in approvals
**Pattern:**
- Placeholder: "Search Employee" or "Select an Employee"
- Typing filters the dropdown in real-time
- Clear button on right side
- Accessibility: `combobox` ARIA role + `status` live region for option count

---

## Validation Patterns

### Client-Side Validation
Triggered on:
- Field blur (leaving a field)
- Form submit attempt

### Error Display
- Error message appears BELOW the field (not as tooltip)
- Field border turns red
- Error text in red
- No inline field icons for error states observed

### Required Field Indicator
- Asterisk (*) on field label
- Typically: "Field Name*" pattern
- Missing required field shows error: likely "[Field Name] cannot be empty" or similar

### Format Validation (Indian-specific)
| Field | Format | Validation Rule |
|-------|--------|----------------|
| PAN | ABCDE1234F | 10 chars: 5 alpha + 4 numeric + 1 alpha; all uppercase |
| Aadhaar | XXXX XXXX XXXX | 12 digits; masked to last 4 in display |
| IFSC | ABCD0123456 | 11 chars: 4 alpha (bank code) + 0 + 6 alphanumeric |
| EPF Number | KA/KAR/1234567/001 | State code / region / establishment / sub-code |
| ESI Number | NN-NN-NNNNNN-NNN-N | Regional code pattern |
| TAN | ABCD12345E | 10 chars: 4 alpha + 5 numeric + 1 alpha |
| Pincode | 110001 | 6 digits |
| Mobile | 10 digits | No spaces; starting with 6/7/8/9 |

### Monetary Validation
- Minimum: ₹0 (cannot be negative)
- Maximum: varies by field (no global cap observed)
- Decimal: 2 decimal places (paisa precision)
- Currency format: ₹X,XX,XXX.XX (Indian number system with lakh/crore separators)

---

## Indian Number Formatting

**Format observed across all monetary displays:**
`₹X,XX,XXX.XX` — Indian number system:
- Last 3 digits before decimal: one group
- Subsequent groups: 2 digits each
- Examples: ₹87,484.00, ₹1,00,000.00, ₹9,45,000.00

This is the standard Indian number formatting (not the Western 3-digit grouping). All monetary displays in the application follow this pattern.

---

## Table Patterns

### Standard Data Table
Used across: Employee list, Pay Run summary, Salary Components, Loans
```
[Table]
  [Header Row: Sortable column headers with sort indicator]
  [Data Rows: Clickable, with hover state]
  [Bulk Actions: Checkbox in first column]
  [Row Actions: Overflow dropdown in last column]
```

**Sortable columns:** Indicated by sort icon (up/down arrows) in column header
**Selectable rows:** Checkbox in first column; "Select all" checkbox in header
**Row navigation:** Click row to open detail panel or navigate to detail page

### Table Empty States
- Single-row message spanning all columns
- Message is contextual to the filter/state

### Table Actions
| Location | Actions |
|----------|---------|
| Header row overflow | Bulk actions (Download All, Export, etc.) |
| Individual row overflow | Per-record actions (Edit, Delete, View, etc.) |
| Above table | Filter, Export Data, search combobox |

---

## Modal and Drawer Patterns

### Modal Dialog (Centered)
**Usage:** Confirmation dialogs, simple forms (Off-Cycle Pay Run initiation)
```
[Backdrop overlay — dims main content]
[Centered dialog box]
  [Close button (X) at top right]
  [Title]
  [Content]
  [Footer: Primary CTA | Cancel]
```

### Drawer/Side Panel
**Usage:** Payslip detail, Employee detail from within Pay Run, Loan detail
```
[Main page remains visible behind]
[Slide-in panel from right side]
  [Close button (X) at top]
  [Panel header with entity name and key stats]
  [Scrollable content sections]
  [Action buttons at bottom]
```

---

## Loading and State Patterns

### Loading State
- ARIA `status` live region announces page loads: "Pay Runs | Summary page loaded"
- Individual section loading likely shows spinner (not captured)

### Disabled State
Observed on:
- "Finalize Payroll" (before preconditions met)
- Form steps that require prior steps (Form 16 Steps 2-4 require Step 1)
- Fields that are auto-populated (read-only calculated fields like Perquisite Rate in Loan form)

### Locked State
Observed on:
- IT Declaration (LOCKED — release required)
- Finalized pay run fields (immutable)

---

## Gaps / Observations
- CSS variable names not inspected — cannot confirm design token names
- No dark mode observed
- Form field focus trapping in modals not confirmed
- Aadhaar masking in API responses not directly observed (only in UI display)
- No loading skeleton/shimmer observed (may exist but not captured in snapshots)

## Open Questions
- [ ] Does the PAN field validate the checksum algorithm (10th character formula)?
- [ ] Is Aadhaar input validated via Luhn or UIDAI format check?
- [ ] What are the CSS custom property names (design tokens) for primary/secondary colors?
- [ ] Are forms auto-saved (draft state) or only saved on explicit Submit?
