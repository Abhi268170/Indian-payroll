# Zoho Payroll — Initiate Exit (Step 1: Exit Details)

**Audited:** 2026-05-24
**Tenant:** lerno (Zoho trial)
**Tested employee:** Arjun Mehta (EMP001)

## 1. Entry points

### 1.1 Where the action lives
Employees list → click employee → Employee Overview page → **kebab menu (⋯ top-right next to "Add" and "X")** → **"Initiate Exit Process"**.

URL pattern when initiated: `/people/employees/{employeeId}/terminate`
Page title: "Exit Process | Zoho Payroll"

### 1.2 Kebab menu items (context-dependent)
The kebab content **changes per employee state**:

| Employee state | Kebab options |
| --- | --- |
| Profile complete, not tax-deductor, Active | Add / Update Vehicle Details · **Initiate Exit Process** |
| Profile complete, IS tax-deductor, Active | Add / Update Vehicle Details · **Initiate Exit Process** (click → blocked by modal — see §2.1) |
| Profile incomplete | Add / Update Vehicle Details · **Delete Employee** (Exit option hidden entirely) |
| "Will be resigned" (exit already in progress) | likely shows "Continue Exit" or similar — not tested yet |

### 1.3 Employee list status badges
Same page (`/people/employees`) shows status column with values observed:
- **Active** (green pill)
- **Will be resigned on dd/MM/yyyy** (orange pill) — exit initiated, last-working-day in future

## 2. Validation gates (blocking)

### 2.1 Tax Deductor block
If the selected employee is currently the org's **Tax Deductor** (Settings → Taxes → Tax Deductor Details):

Modal shown — title "**You can't initiate the exit process**":
> You cannot initiate the exit process for **{Employee Name}** as the employee is the Tax Deductor for your organisation. To change the tax deductor details, go to Settings → Taxes.

Modal has a single **Okay** button. No exit form opens.

**Workaround:** Reassign the Tax Deductor first (Settings → Taxes → Tax Details). Two options:
- Pick a different **Employee** from the Deductor's Name dropdown, or
- Switch Deductor's Type to **Non-Employee** and supply Name / Father's Name / Designation (three required text fields).

After save, kebab reveals "Initiate Exit Process" for the previously-blocked employee.

### 2.2 Profile completeness block
If the employee's profile is incomplete (e.g. missing DOB, Father's Name, Bank Account), the kebab does **not** show "Initiate Exit Process" at all — only "Delete Employee".

## 3. Exit details form (Step 1 of N)

### 3.1 Layout
- **Left column** — form
- **Right column** — employee summary card: avatar, Name, ID, Designation, Department, Date of Joining
- Footer: **Proceed** (primary, blue) · **Cancel** (secondary, white)

### 3.2 Form fields

| Order | Label | Required | Control type | Default | Notes |
| --- | --- | --- | --- | --- | --- |
| 1 | Last Working Day | Yes | Date picker (`dd/MM/yyyy`) | empty | Calendar opens to current period (e.g. June 2026 if "today" is May 2026). All days of the open month are selectable (past + future). Standard month navigation arrows. |
| 2 | Reason for Exit | Yes | Searchable dropdown (custom — not native `<select>`) | empty ("Select") | See §3.3 |
| 3 | When do you want to settle the final pay? | n/a (always one selected) | Radio group, 2 options | "Pay as per the regular pay schedule" | See §3.4 |
| 4 | Final Settlement Date | Conditional (Yes when radio = "Pay on a given date") | Date picker (`dd/MM/yyyy`) | empty | Appears below the radio group only when "Pay on a given date" is selected. |
| 5 | Personal Email Address | No | Text input | empty | Tooltip on info-circle icon: **"Enter the email address to which you want to send this employee's final payslip and Form-16"** |
| 6 | Notes | No | Textarea | empty | Free text, no length indicator visible. |

### 3.3 Reason for Exit — values (in dropdown order)
1. **Terminated By Employer** (highlighted first / default suggestion)
2. **Termination By Death**
3. **Termination by Disability**
4. **Resigned By Employee**

Dropdown also has a built-in **search box** at the top — implies the enum may grow, or this is a generic Zoho control.

### 3.4 Settlement timing radio

- **Pay as per the regular pay schedule** — default. No additional field appears. Final pay will be released with the next regular pay run that covers the exit period.
- **Pay on a given date** — reveals the required **Final Settlement Date** date picker. Used when employer wants to release FnF on a specific off-cycle date instead of waiting for the next run.

### 3.5 Note banner (always visible at form bottom, above footer)
Light-amber background, prefixed with bullet:

> **Note:**
> - Portal is not enabled for this employee. Kindly collect the proof of investments before processing the payroll.

(Banner is contextual on portal-disabled employees. Likely hidden if employee portal is enabled — to verify on an employee whose portal access is enabled.)

## 4. Side panel — employee summary card

Right-hand fixed card during the exit flow shows:
- Avatar (initials, large)
- Full Name
- "ID: {EmployeeCode}"
- Designation
- Department
- Date of Joining (`dd/MM/yyyy`)

No edit affordance — purely contextual.

## 5. URL & navigation observations

- Entering the exit flow pushes the URL to `/people/employees/{id}/terminate` — survives browser back/forward.
- Cancel button likely returns to `/people/employees/{id}` overview (not yet tested).
- Browser back from the exit form returns to overview without confirmation prompt (not yet tested for unsaved-data warning).

## 6. Open questions / next-step observations

- The dropdown's search box implies more reasons may be hidden behind text filter, OR is a stock Zoho component — confirm via API or extended typing tests.
- "Pay on a given date" — what happens if `Final Settlement Date < Last Working Day`? Likely client-side block — verify.
- "Pay on a given date" relative to current period — can it cross fiscal years? Affects TDS / Form-16 generation.
- Step 2+ of the wizard (after Proceed) not yet captured — likely covers: salary settlement preview, statutory recoveries (PF/Gratuity), leave encashment, notice pay, recoveries, deductions, FnF document send.
- Portal-enabled employee variant — note banner may change or disappear.
- "Will be resigned" badge on Priya Sharma — implies after Step 1 submission, employee enters a new state but stays Active until last working day; clicking such an employee likely shows a "Continue Exit" or "Cancel Exit" affordance, not yet tested.

## 7. Screenshots captured

- `zoho-arjun-overview.png` — overview before exit
- `zoho-arjun-kebab.png` — kebab open showing "Initiate Exit Process"
- `zoho-initiate-exit.png` — tax-deductor block modal
- `zoho-settings-taxes.png` — Tax Details settings
- `zoho-tax-nonemp.png` — Non-Employee deductor form
- `zoho-tax-filled.png` — filled before save
- `zoho-tax-after-save.png` — post-save state
- `zoho-arjun-kebab2.png` — kebab after reassignment (Initiate Exit Process now usable)
- `zoho-exit-step1.png` — Step 1 default state
- `zoho-exit-reasons.png` — Reason dropdown expanded
- `zoho-exit-paydate.png` — "Pay on a given date" reveals Final Settlement Date
- `zoho-exit-tooltip.png` — personal email tooltip
- `zoho-exit-datepicker.png` — calendar popup
