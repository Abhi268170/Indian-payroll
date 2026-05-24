# Settings > Users

## URL
`#/settings/users-roles/users`

Sub-routes:
- `#/settings/users-roles/invite` — Invite User form
- `#/settings/users-roles/{id}/edit` — Edit User form

## Purpose
Manages access to the Zoho Payroll application. Admins can invite additional users (HR managers, accountants, approvers) and assign them roles that define their access permissions.

## Page Layout
Table listing all users with columns: User Details | Role | Status | More Actions.
Header: "Users" heading + "Invite User" button.

**Current state:** One user exists — the account owner (Admin).

## Fields

### Users List Table
| Column | Contents |
|--------|----------|
| User Details | Avatar initial (auto-generated from name), display name (link to edit), email address |
| Role | Assigned role name (e.g., "Admin") |
| Status | "ACTIVE" or "INACTIVE" badge |
| More Actions | Three-dot dropdown button |

### Invite User Form (`#/settings/users-roles/invite`)
| Field | Type | Required | Default | Options / Format | Notes |
|-------|------|----------|---------|------------------|-------|
| Name | Text | Yes | (blank) | Free text | Display name of the invited user |
| Email | Email | Yes | (blank) | Valid email format | Invitation will be sent to this address |
| Role | Dropdown | Yes | (blank — must select) | Admin, Manager, Reimbursements and POI Reviewer | Determines module access permissions |

### Edit User Form (`#/settings/users-roles/{id}/edit`)
| Field | Type | Required | Default | Notes |
|-------|------|----------|---------|-------|
| Name | Text | Yes | Current name | Editable |
| Email | Email / Read-only | Yes | Current email | Pre-filled; likely read-only (cannot change email of existing Zoho account) |
| Role | Dropdown | Yes | Current role | Same options as Invite form |

## Buttons & Actions

### Users List
| Button | Label | State | Action |
|--------|-------|-------|--------|
| Invite User | "Invite User" | Always enabled | Navigates to `#/settings/users-roles/invite` |
| User name link | (username) | Always enabled | Navigates to `#/settings/users-roles/{id}/edit` |
| More Actions (own account) | Three-dot | Enabled | Shows: "Edit" only (cannot delete yourself) |
| More Actions (other users) | Three-dot | Enabled | Expected: Edit, Deactivate/Remove (not observed — only one user exists) |

### Invite User Form
| Button | Label | State | Action |
|--------|-------|-------|--------|
| Preview invitation template | Link + icon | Always enabled | Opens a preview of the email that will be sent to the invited user |
| Close (X) | Link | Always enabled | Navigates back to `#/settings/users-roles/users` |
| Invite User | "Invite User" | Enabled when all required fields filled | Sends invitation email; creates user record with "Pending" status |
| Cancel | "Cancel" | Always enabled | Returns to users list |

## Tabs (if any)
None on Users page. "Users" and "Roles" are two separate links in the left nav under "Users and Roles" section.

## Conditional Logic
1. **Own account dropdown** — only shows "Edit"; no deactivation or deletion of the logged-in admin account.
2. **Role dropdown** — shows only the three system-defined roles plus any custom roles created in the Roles page.
3. **"Preview invitation template"** — available on both Invite and Edit forms; lets admin see the email content before sending.

## Cross-Module Impact
| Setting | Impacts |
|---------|---------|
| User Role assignment | Controls which pages/actions the user can access across ALL Zoho Payroll modules |
| "Manage Users" permission in role | Determines if a user can access this settings page |
| User invitation | Creates a pending user linked to the Zoho account ecosystem; user must accept invite via email |

## Observations & Notes
1. **Zoho account-level identity** — users are Zoho account holders, not standalone payroll app users. Inviting a user sends a Zoho account invitation. This means user identity management is outsourced to Zoho's identity platform.
2. **Three built-in roles**: Admin (full access), Manager (all modules except org settings), Reimbursements and POI Reviewer (narrow: only reimbursements and POI approvals).
3. **No MFA/2FA settings here** — MFA would be managed at the Zoho account level, not within Payroll settings.
4. **No IP restriction, session timeout, or SSO settings** visible on this page — likely handled at Zoho account level.
5. For our own build: User management must include: invite-by-email, role assignment, deactivate/reactivate, session management, and optionally SSO via SAML/OIDC for enterprise tenants.

## Screenshots
- `docs/ba-audit/settings/screenshots/07-users-list.png` — user list
- `docs/ba-audit/settings/screenshots/07-users-invite.png` — invite user form
