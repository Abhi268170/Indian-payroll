# Item 97: Professional Tax (PT) Settings

**URL:** `https://payroll.zoho.in/#/settings/statutory-details/list/pt`  
**Revise Slabs URL:** `https://payroll.zoho.in/#/settings/statutory-details/{location_id}/pt-details`  
**Module:** Settings > Statutory Components  
**State in lerno org:** Configured (Kerala, Half Yearly)

---

## Screenshots

- `screenshots/97-pt-kerala-slabs.png` — PT Kerala slabs modal
- `screenshots/97-pt-revise-slabs-form.png` — PT Revise Slabs form

---

## PT Settings List View

**URL:** `#/settings/statutory-details/list/pt`

Page heading: "Professional Tax"  
Descriptive text: "This tax is levied on an employee's income by the State Government. Tax slabs differ in each state."

### Location Card: Head Office

| Field | Value | Editable |
|-------|-------|----------|
| PT Number | (empty — not entered) | Via "Update PT Number" button |
| State | Kerala | No (set by org HQ location) |
| Deduction Cycle | Half Yearly | No (state-specific, not configurable) |
| PT Slabs | Displayed via "View Tax Slabs" button | Via "(Revise)" button |

**Note:** PT is work-location scoped. Each work location (Head Office, branches) has its own PT card. The state shown matches the location's state. If the org has branches in different states, each branch has separate PT config with state-specific slabs and deduction cycles.

**"Update PT Number" button:** Opens an inline edit to enter/update the PT TIN/registration number for this location. Not a modal — inline edit observed.

---

## PT Slabs Modal — View Tax Slabs

**Triggered by:** "View Tax Slabs" button on the location card

**Modal Title:** "Tax Slabs for Head Office"

### Displayed Fields (read-only):

| Field | Value |
|-------|-------|
| Deduction Cycle | Half Yearly |
| Effective from | 01/04/2026 |

### Kerala PT Slabs (Half Yearly Gross Salary basis):

| Half Yearly Gross Salary (₹) | Half Yearly Tax Amount (₹) |
|------------------------------|---------------------------|
| 1 – 11,999 | 0 |
| 12,000 – 17,999 | 320 |
| 18,000 – 29,999 | 450 |
| 30,000 – 44,999 | 600 |
| 45,000 – 99,999 | 750 |
| 1,00,000 – 1,24,999 | 1,000 |
| 1,25,000 – 99,99,99,999 | 1,250 |

**Maximum annual PT for Kerala:** ₹1,250 × 2 = ₹2,500/year (Half Yearly deduction)  
**Statutory cap:** Kerala PT maximum is ₹2,500/year (Kerala Panchayat Raj Act / Kerala Municipal Act).

**Close action:** "Okay" button dismisses modal.

---

## PT Slab Revision Form

**URL:** `#/settings/statutory-details/{location_id}/pt-details`  
**Triggered by:** "(Revise)" button on the location card

### Form Structure

**Heading:** "Professional Tax,"

| Field | Type | Required | Value | Editable |
|-------|------|----------|-------|----------|
| Deduction Cycle | Text (disabled) | N/A | Half Yearly | No — state-mandated |
| Effective From | Date picker | Yes | 01/06/2026 (default shown) | Yes |

**Tax Slabs Table:** Inline editable grid

| Column | Type | Notes |
|--------|------|-------|
| Start Range (₹) | Spinbutton | First row = 1 (disabled/locked — always starts at 1) |
| (dash separator) | Static | Visual separator |
| End Range (₹) | Spinbutton | User-editable |
| Half Yearly Tax Amount (₹) | Spinbutton | User-editable |
| (delete button) | Button | Remove slab row (icon button, not available on first row) |

**Current slabs loaded (pre-populated from existing Kerala config):**
- Row 1: 1 – 11,999 = ₹0 (Start locked to 1)
- Row 2: 12,000 – 17,999 = ₹320
- Row 3: 18,000 – 29,999 = ₹450
- Row 4: 30,000 – 44,999 = ₹600
- Row 5: 45,000 – 99,999 = ₹750
- Row 6: 1,00,000 – 1,24,999 = ₹1,000
- Row 7: 1,25,000 – 99,99,99,999 = ₹1,250

**"Additional Slab" button:** Adds a new editable row at the bottom of the slab table.

### Actions

| Action | Type | Behavior |
|--------|------|----------|
| Save | Button | Saves new slabs with Effective From date. Creates a new slab revision record (not overwriting — effective from date). |
| Cancel | Link | Returns to `#/settings/statutory-details/list/pt` without saving |
| Delete slab (icon button per row) | Button | Removes that slab row. Not available on first row. |
| Additional Slab | Button | Appends new empty row to slab table |

---

## Business Rules

1. **PT is state-specific:** Slabs, rates, and deduction cycle differ by state. Zoho pre-populates state-specific defaults.
2. **PT is location-scoped:** Each work location has its own PT config. Org with branches in multiple states has multiple PT configs.
3. **Deduction cycle is state-mandated:** Kerala = Half Yearly. Cannot be changed by admin.
4. **Slab revision is versioned:** "Effective From" date means new slabs apply from that date forward. Old slabs retained for historical pay runs.
5. **"Start Range" of first slab is locked at 1** — no gap at the bottom.
6. **Last slab's end range** = very large number (99,99,99,999 = ~₹10 crore) to catch all high salaries.
7. **Kerala maximum PT:** ₹2,500/year (₹1,250 × 2 half-yearly installments). Compliant with Kerala PT statute.
8. **PT deduction basis:** Half Yearly Gross Salary (not monthly) for Kerala — system accumulates bi-annual.

---

## Statutory References

- Kerala Panchayat Raj Act, 1994 / Kerala Municipality Act, 1994 (PT provisions)
- Professional Tax rates are set by individual state legislatures — varies by state
- PT is deductible under Sec 16(iii) of Income Tax Act (allowed as deduction from gross salary)
- Maximum PT cap: ₹2,500/year (per Article 276 of Constitution of India)

---

## Cross-Module Impact

- PT deduction appears in employee payslip under "Deductions" section
- PT challan generation available in Compliance module
- PT state assignment comes from employee's work location (not residential address)
- PT deducted from employee salary (no employer PT contribution)

---

## Open Questions

- [ ] How are branches/additional locations added? Is there a "Work Locations" settings page?
- [ ] If an employee works in a state with monthly PT (e.g., Maharashtra), is the deduction cycle shown as Monthly?
- [ ] Can the admin configure PT for states that have no PT (e.g., Delhi, Haryana)? Is PT simply not shown?
- [ ] What is the column header label when a state has Monthly vs Half Yearly — does "Half Yearly Gross Salary" change to "Monthly Gross Salary"?
- [ ] Is the "Effective From" date for slab revision validated to not be earlier than the current pay period?
- [ ] Can multiple slab revisions be queued (e.g., FY2026 slabs + FY2027 slabs both saved in advance)?
