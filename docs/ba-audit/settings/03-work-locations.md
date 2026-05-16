# Settings > Work Locations

## URL
`#/settings/work-locations`

Sub-routes:
- `#/settings/work-locations/new` — Add new work location
- `#/settings/work-locations/{id}/edit` — Edit existing work location

## Purpose
Manages the physical office locations of the organisation. Each work location has a state, which drives state-specific statutory compliance: Labour Welfare Fund (LWF) and Professional Tax (PT) applicability, and filing address assignment.

## Page Layout
List page showing all work location cards. Each card shows:
- Location name
- Address (lines, city, state, PIN)
- "Filing Address" badge (if designated as filing address)
- Employee count
- Edit link (pencil icon)
- Context menu (three-dot button) with Delete and Mark as Inactive options

Header: "Work Locations" heading + "Add Work Location" button + a sort/filter icon button.

**Current state:** One location exists — "Head Office" with 0 employees assigned.

## Fields

### Work Location List Card (read-only display)
| Field | Displayed |
|-------|-----------|
| Location Name | "Head Office" |
| Address Line 1 | lerno |
| Address Line 2 | kazhakoottam |
| City, State, PIN | thiruvananthapuram, Kerala 695010 |
| Filing Address badge | Shown (this is the designated filing address) |
| Employee count | 0 Employees |

### Add / Edit Work Location Form

| Field | Type | Required | Default | Options / Format | Notes |
|-------|------|----------|---------|------------------|-------|
| Work Location Name | Text | Yes | (blank for new) | Free text | Displayed on employee profiles and reports |
| Address Line 1 | Text | No | (blank for new) | Free text | Part of the work location address |
| Address Line 2 | Text | No | (blank for new) | Free text | Optional second address line |
| State | Dropdown | Yes | (blank for new) | All 37 Indian states/UTs | **Disabled on edit** — cannot be changed after creation |
| City | Text | No | (blank for new) | Free text | City name |
| PIN Code | Text | No | (blank for new) | 6-digit numeric | Postal code |

### Critical Statutory Note (shown in Edit form when state = Kerala)
> "Labour Welfare Fund is applicable for Kerala. If you've not configured it yet, configure it in **Settings > Statutory Components**."

This notice is state-dependent — it appears because Kerala mandates LWF. Other states with LWF (e.g., Maharashtra, Karnataka) would trigger similar notices.

## Buttons & Actions

### List Page
| Button | Label | State | Action |
|--------|-------|-------|--------|
| Add Work Location | "Add Work Location" | Always enabled | Navigates to `#/settings/work-locations/new` |
| Sort/Filter icon | (icon only) | Always enabled | Unknown — likely sorts/filters the location list |
| Edit (pencil icon) | "Edit [Location Name]" | Always enabled | Navigates to `#/settings/work-locations/{id}/edit` |
| Show dropdown menu | Three-dot icon | Always enabled | Opens context menu with: **Delete**, **Mark as Inactive** |

### Context Menu Options
- **Delete** — Permanently deletes the work location (likely blocked if employees are assigned)
- **Mark as Inactive** — Soft-deactivates the location without deletion

### Add/Edit Form
| Button | Label | State | Action |
|--------|-------|-------|--------|
| Save | "Save" | Always enabled | Creates/updates the work location record |
| Cancel | "Cancel" | Always enabled | Discards changes and navigates back to `#/settings/work-locations` |

## Tabs (if any)
None. Single list view + detail form.

## Conditional Logic

1. **State field disabled on edit** — Once a work location is created with a state, the state cannot be changed. This enforces statutory integrity (changing state would invalidate existing PT/LWF configurations for employees at that location).
2. **LWF/PT notice on edit form** — Displayed conditionally based on the state selected. Shown for Kerala because Kerala has LWF. The message links the user to `Settings > Statutory Components` to configure it.
3. **Filing Address badge** — Shown only on the location designated as the filing address in Org Profile settings.
4. **Employee count** — Shown on the list card. Likely blocks deletion when count > 0.

## Cross-Module Impact

| Setting | Impacts |
|---------|---------|
| Work Location State | Determines PT (Professional Tax) slab applicability for employees at that location — PT is state-specific |
| Work Location State | Determines LWF (Labour Welfare Fund) applicability — LWF is mandatory in specific states (Kerala, Maharashtra, Karnataka, etc.) |
| Work Location | Assigned to each employee record; appears on payslips and statutory filings |
| Filing Address designation | The filing address work location address is printed on all statutory forms and payslips |
| Employee assignment | Employees are assigned to a work location; their statutory deductions (PT, LWF) derive from the location's state |

## State Machine

```
New (blank form)
  → Save → Active (shown in list)
  → Cancel → Discarded

Active
  → Edit → (modified) → Save → Active (updated)
  → Mark as Inactive → Inactive
  → Delete → Deleted (if 0 employees assigned)

Inactive
  → (presumably can be reactivated — not confirmed from UI)
```

## Observations & Notes

1. **State immutability on edit** is a strong statutory design decision. Changing a location's state mid-year would invalidate PT and LWF calculations for all employees at that location. This is correct behavior.
2. **LWF notice is contextual and actionable** — it points directly to the Statutory Components settings page. This is good UX for compliance guidance.
3. **No phone number or email field** on the work location — Zoho Payroll does not capture contact details per location (unlike some payroll systems that require branch contact info for TDS/PF filings).
4. **"Head Office" is auto-created** from the Organisation Profile address when the org is set up. This is the default primary work location.
5. **Filing Address lives at org level** but is linked to a specific work location. The `?change_filing_address=true` query param on the org profile page suggests this is where the link between org filing address and a work location is managed.
6. **Employee count shown** on the card but not in real time (shows 0 for the Head Office despite the org being set up). This may be a stale count or reflect that no employees have been formally assigned yet.
7. **ID format**: Work location IDs are long numeric strings (e.g., `3848927000000032281`) — suggests Zoho's internal ID generation scheme.
8. For our own implementation: Work location must be a first-class entity with state as an immutable attribute post-creation. State drives PT slab lookup and LWF applicability checks. Deletion should be blocked if employees are assigned.

## Screenshots
- `docs/ba-audit/settings/screenshots/03-work-locations.png` — list view
- `docs/ba-audit/settings/screenshots/03-work-locations-new.png` — add/edit form
