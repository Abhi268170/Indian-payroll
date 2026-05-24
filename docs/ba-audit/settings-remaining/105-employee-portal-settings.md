# Item 105: Employee Portal Settings

**URL:** `https://payroll.zoho.in/#/settings/portal/preferences`
**Module:** Settings > Employee Portal
**Sub-sections:** Preferences | Web Tabs
**Audit Date:** 2026-05-15

---

## Screenshots

- `screenshots/105-employee-portal-settings.png` — Employee Portal Preferences page (full)

---

## Employee Portal Settings — Navigation

**Parent Settings section:** Employee Portal (sidebar under Organisation Settings)

**Sub-nav:**
| Page | URL |
|------|-----|
| Preferences | `#/settings/portal/preferences` |
| Web Tabs | `#/settings/portal/webtabs` |

**Quick Links (sidebar, visible on portal settings pages):**
- Flexible Benefit Plan
- Reimbursement Claims
- Income Tax Declaration
- Proof Of Investments

---

## Preferences Page

**URL:** `#/settings/portal/preferences`

### Enable Portal Access Toggle

| Field | Type | Current State | Description |
|-------|------|---------------|-------------|
| Enable Portal Access | Toggle | ACTIVE | Enables the employee self-service portal. Employees can access salary info, FBP declarations, and POI submission. |

**Toggle description text:**
> "The employee portal allows your employees to access their salary information and perform payroll related activities like declaring their Flexible Benefit Plan (FBP) and submitting investment proofs for approval."

### Banner Message Section

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Enter Banner Message | Textarea | No | Free text; shown at top of Home page in Employee Self-service Portal |
| Select till when this message must be displayed | Date picker | Conditional | Required when banner message is entered; sets expiry date for the banner |

**"View Sample Preview"** link — shows how the banner appears in the portal.

**Business rule:** Banner message appears on the employee portal home page until the specified date expires. Good for org announcements (e.g., payslip release notice, IT declaration deadline).

### Portal Contact Information

| Field | Type | Notes |
|-------|------|-------|
| Contact Name | Display | Shows user name (abhijithss2255) |
| Contact Email | Display | Shows email for employee queries (abhijithss2255@gmail.com) |
| Manage Contacts | Button/Link | Opens `#/settings/portal/preferences?configure_emails=true` — Manage Contact panel |

**Manage Contacts Panel (opens inline via `?configure_emails=true`):**

| Section | Content |
|---------|---------|
| CONTACT INFORMATION | Contact name (abhijithss2255) |
| SHOWN IN PORTAL | Email shown to employees |
| + Add Contact | Adds additional contact |

**Business rule:** This email is shown in the portal for employees to direct queries. Not the sending email — that is Sender Email Preferences.

### Document Management

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| Show documents in employee portal | Toggle/Checkbox | (off) | Makes Documents module visible to employees in the portal |

**Description text:**
> "Enable this option to make documents visible for employees to access in the employee portal."

**Business rule:** Documents are controlled separately in the Documents module (Employee Folder vs Org Folder). This toggle is a global on/off gate — without it, even employee-scoped documents are hidden from the portal.

### Actions

| Action | Trigger | Behavior |
|--------|---------|----------|
| Save | Button (primary) | Saves all preferences on the page |

---

## Web Tabs Page

**URL:** `#/settings/portal/webtabs`

### Empty State

**Heading:** "You haven't created any web tab yet"

**Description:**
> "Create a web tab to help employees quickly access external sites like company policies, learning platforms, and other resources—right from their Employee Portal."

### Actions

| Action | Description |
|--------|-------------|
| Create New Web Tab | CTA button — opens web tab creation form (not audited — empty state only) |
| Create New Web Tab (toolbar) | Same action, toolbar button |

**Business purpose:** Embed external URLs (e.g., company intranet, policy documents, LMS) as tabs within the employee portal navigation.

---

## Business Rules

1. **Portal is enabled by default** (ACTIVE state observed) — employees can access the portal without admin action.
2. **Banner message is time-bounded** — expiry date required if banner set; prevents stale announcements.
3. **Document visibility is gated separately** — `Show documents in employee portal` must be enabled AND document must be in an Employee Folder for the employee to see their documents in the portal.
4. **Portal contact is distinct from sender email** — contact info shown to employees for queries; sending email is configured under Sender Email Preferences.
5. **Web Tabs** are custom embedded links — not payroll-function tabs; useful for L&D, policy, HR links.
6. **Single contact per portal by default** — `+ Add Contact` allows multiple contacts (e.g., HR + Payroll contacts).

---

## Cross-Module Impact

- Portal Preferences → Documents module: `Show documents` toggle gates employee portal document access
- Portal Preferences → IT Declaration / POI: employees can submit via portal (controlled from Claims and Declarations Settings, not here)
- Portal Preferences → FBP: employees can declare FBP via portal (controlled from FBP preference settings, not here)
- Web Tabs → external systems: no data dependency; pure navigation links

---

## Data Relationships

- Portal contact → User record (contact email shown = admin user email; `Manage Contacts` lets adding others)
- Web Tab → external URL (no foreign key to internal data)

---

## Open Questions

- [ ] Can employees view all employees' documents (Org Folder) or only their own (Employee Folder) when `Show documents` is enabled?
- [ ] Can the portal be restricted to specific employees (e.g., disable portal for a subset)?
- [ ] What is the Web Tab creation form — what fields (tab name, URL, icon)?
- [ ] Is the portal a separate subdomain (e.g., `employee.zoho.in`) or the same `payroll.zoho.in` with different auth?
- [ ] Does `+ Add Contact` allow adding non-admin users as portal contacts?
