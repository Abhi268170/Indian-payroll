# UF-A6: Employee Exit and Full & Final Settlement Flow

**Module:** Employees > Employee Profile > Initiate Exit Process
**Tested:** 2026-05-16
**Approach:** Tested on Priya Sharma (EMP002) — the only non-Tax-Deductor employee with payroll history eligible for exit. Navigated to `#/people/employees/{id}/terminate`. Explored all form fields and dropdowns. Did NOT submit final exit (task instruction: document fields without completing exit).

---

## Findings

### 1. Prerequisites and Eligibility

**"Initiate Exit Process" menu item availability:**

| Condition | Dropdown Option Shown |
|-----------|----------------------|
| Employee has NO payroll history | "Delete Employee" (hard delete) |
| Employee has payroll history | "Initiate Exit Process" |
| Employee is Tax Deductor | "Initiate Exit Process" (visible but blocked on submit) |

**Business Rule — Tax Deductor Block:**
When "Initiate Exit Process" is clicked for the Tax Deductor employee, the system shows:
> "You cannot initiate the exit process for Arjun Mehta as the employee is the Tax Deductor for your organisation."

Resolution: Admin must first reassign Tax Deductor in Settings > Tax Details before exiting that employee.

---

### 2. Exit Form — Entry Point

**Trigger:** Employee profile page → "Show dropdown menu" button → "Initiate Exit Process"

**Route after click:** `#/people/employees/{employeeId}/terminate`

**Page title:** "Exit Process | Zoho Payroll"

---

### 3. Exit Form — Step 1: Exit Details

**Form heading:** "Priya Sharma's Exit details"

**Fields:**

| Field | Type | Required | Options / Format | Notes |
|-------|------|----------|-----------------|-------|
| Last Working Day | Date input | Yes | dd/MM/yyyy | Ember date picker; field validation fires on Proceed if empty. Error: "You've forgotten to enter the last working date." |
| Reason for Exit | Custom dropdown | Yes | See options below | Ember autocomplete-select (`.ac-selected`) |
| Personal Email Address | Text input | No | Free text email | For post-exit communication; employee loses org email access |
| Notes | Textarea | No | Free text | Internal notes on exit; not sent to employee |

**Reason for Exit Options (complete enumeration):**

| Option | Notes |
|--------|-------|
| Terminated By Employer | Involuntary exit — company-initiated |
| Termination By Death | Employee deceased |
| Termination by Disability | Medical incapacitation |
| Resigned By Employee | Voluntary resignation |

**Observation:** No "Retirement" or "Contract End" reason — these are common in Indian payroll. Gratuity eligibility differs by reason. 🟡

---

### 4. Exit Form — Final Pay Settlement Section

**Sub-heading:** "When do you want to settle the final pay?"

**Radio button options:**

| Option | Value | Description |
|--------|-------|-------------|
| Pay as per the regular pay schedule | false (default, pre-selected) | F&F salary added to next regular pay run |
| Pay on a given date | true | Admin specifies a custom date for F&F disbursement |

**When "Pay on a given date" selected:**
- An additional date picker appears for the custom payment date
- Format: dd/MM/yyyy

**Default selection:** "Pay as per the regular pay schedule" (radio value = false = checked by default)

---

### 5. Employee Info Panel (Right Side)

On the exit form, the employee's current info is shown in a side panel:

| Field | Value (Priya Sharma) |
|-------|---------------------|
| ID | EMP002 |
| Designation | Junior Developer |
| Department | Engineering |
| Date of Joining | 16/05/2025 |

---

### 6. Form Validation

**Validation trigger:** Clicking "Proceed" button

**Validation rules observed:**

| Field | Validation Rule | Error Message |
|-------|----------------|---------------|
| Last Working Day | Required | "You've forgotten to enter the last working date." |
| Reason for Exit | Required (inferred) | Not directly tested |

**Validation style:** Alert box/banner within the form (not toast notification), showing:
> "Oops! Looks like you missed something... You've forgotten to enter the last working date."

**Note on date field behaviour:** The Ember date picker component does not accept programmatic value injection via native `HTMLInputElement.prototype.value` setter — it requires actual UI keyboard interaction (type characters) or datepicker calendar clicks to register the date in Ember's internal state. Value shown in DOM (`input.value`) may differ from Ember's model value.

---

### 7. Proceed Button

| Attribute | Value |
|-----------|-------|
| Button label | "Proceed" |
| CSS class | `btn btn-primary ember-view` |
| Disabled state | Never disabled pre-validation — validates on click |
| Post-success navigation | Expected: F&F computation step (not tested) |

---

### 8. Expected F&F Computation (Not Tested — Inferred from Zoho Payroll Domain Knowledge)

After "Proceed" with valid exit details, the system is expected to show:

**Step 2: F&F Computation Summary**
- Regular salary proration for partial month (April 1 to Last Working Day)
- Leave encashment (if leave balance exists and encashment is configured)
- Gratuity (if employee has >= 5 years of service)
- Notice pay recovery (if notice period not served)
- Outstanding loan deductions
- Reimbursement payout (if pending approved claims)
- TDS recalculation for the shortened year

**Step 3: Final settlement pay run creation**
- Creates a special "Exit" pay run type

**Not tested:** Could not proceed past Step 1 due to Ember date picker incompatibility with programmatic input.

---

### 9. Delete Employee (No Payroll History)

For employees with no payroll history (EMP003, EMP004, EMP005), the dropdown shows "Delete Employee" instead of "Initiate Exit Process".

**Expected behaviour:** Hard deletes the employee record. No F&F computation (no payroll history). Likely shows a confirmation modal before deletion.

**Not tested:** Did not click "Delete Employee" to avoid accidentally deleting test data.

---

### 10. Navigation Paths

| From | To |
|------|----|
| Employee profile page | Exit form (`#/terminate`) via dropdown |
| Exit form Proceed | F&F computation step (next step — not tested) |
| Exit form Cancel / Back | Returns to employee profile |
| Tax Deductor block error | Stays on employee profile (no navigation) |

---

## Screenshots / Files

- `exit-process-form.png` — Exit form with all fields visible
- `exit-reason-dropdown.png` — Reason for Exit dropdown open (all 4 options)
- `exit-form-filled.png` — Form filled with date + reason selected
- `exit-form-validation-error.png` — Validation error banner after Proceed click
- `aisha-khan-dropdown.png` — Aisha Khan (EMP004) showing "Delete Employee" instead of Initiate Exit
- `exit-blocked-tax-deductor.png` — Tax Deductor exit block error (prior session)

---

## Gaps / Open Questions

- [ ] **F&F computation step:** What fields / calculations appear after successful Proceed? Especially: gratuity, notice pay recovery, leave encashment.
- [ ] **Gratuity eligibility check:** Does the system compute gratuity automatically? What triggers the gratuity line item (5-year tenure rule under Gratuity Act 1972)?
- [ ] **"Retirement" reason missing:** Common exit reason not available. Is this an oversight or handled differently (e.g., through a separate Retirement module)?
- [ ] **"Contract End" reason missing:** For fixed-term contractors, there's no "Contract End" reason. 🟡
- [ ] **Notice period field:** The form does not have an explicit "Notice period served" field — is this inferred from Last Working Day vs resignation date?
- [ ] **F&F pay run type:** Does the F&F settlement create a pay run visible in Pay Runs list? What type/label does it get?
- [ ] **Gratuity for < 5 years:** What happens for employees with < 5 years tenure? Is gratuity shown as ₹0 or hidden?
- [ ] **Delete Employee confirmation modal:** What warning text appears before hard deletion? Is this reversible?
- [ ] **Full-page date picker interaction:** The Ember date picker requires clicking calendar cells, not programmatic fill. A user flow video/manual test would capture this better.
