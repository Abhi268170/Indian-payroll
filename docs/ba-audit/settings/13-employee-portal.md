# Settings > Employee Portal

## URL
`#/settings/portal/preferences`

## Purpose
Controls the Employee Self-Service (ESS) portal — whether it is active, what banner message is shown, which contact email handles employee queries, and whether documents are visible to employees.

## Page Layout
Two tabs: **Preferences** (current) | **Web Tabs**
Right sidebar: Quick Links to FBP, Reimbursement Claims, Income Tax Declaration, Proof of Investments.

## Fields

| Field | Type | Required | Default / Current | Notes |
|-------|------|----------|-------------------|-------|
| Enable Portal Access | Toggle | No | ACTIVE (enabled) | Enables/disables the entire employee self-service portal |
| Banner Message | Textarea | No | (blank) | Text shown at top of employee portal home page; "View Sample Preview" link available |
| Select till when message is displayed | Date picker | No | (blank) | Expiry date for the banner message |
| Portal Contact Information | Display + link | N/A | abhijithss2255@gmail.com | Email for employee queries; "Manage Contacts" link to change |
| Show documents in employee portal | Checkbox/Toggle | No | (unknown — UI not confirmed) | Makes uploaded HR documents visible to employees in the portal |

## Buttons & Actions
| Button | Action |
|--------|--------|
| Enable Portal Access toggle | Activates/deactivates the entire employee portal |
| View Sample Preview | Opens a preview of what the portal banner looks like |
| Manage Contacts | Opens contact management for portal query email |
| Save | Saves all preferences |

## Tabs
- **Preferences** — portal enable/disable, banner, contact, document access
- **Web Tabs** — custom web tabs embedded in the employee portal (requires subscription feature)

## Quick Links (right sidebar)
- Flexible Benefit Plan — navigates to FBP settings
- Reimbursement Claims — navigates to claims settings
- Income Tax Declaration — navigates to IT declaration settings
- Proof of Investments — navigates to POI settings

## Cross-Module Impact
| Setting | Impacts |
|---------|---------|
| Enable Portal Access | When disabled, employees cannot log in to view payslips, submit declarations, or check their salary |
| Banner Message | Shown on every employee's portal home page until the expiry date |
| Portal Contact | Employee queries via portal go to this email |
| Show documents | Controls whether HR-uploaded documents (offer letters, policies) are visible to employees |

## Observations & Notes
1. **Portal is ACTIVE by default** — employees can access self-service from day one of org setup.
2. **Banner with expiry date** — useful for compliance deadlines ("Submit IT declarations by 31 Jan").
3. **Web Tabs tab** — allows embedding custom URLs inside the employee portal (e.g., a custom benefits portal). Requires subscription.
4. For our build: ESS portal enable/disable should be a tenant-level flag. Banner message should support rich text and expiry date.

## Screenshots
`docs/ba-audit/settings/screenshots/13-employee-portal.png`
