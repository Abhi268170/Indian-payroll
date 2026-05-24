# Item 102: User Management & RBAC

**URL:** `https://payroll.zoho.in/#/settings/users-roles/users`  
**Module:** Settings > Organisation Settings > Users and Roles  
**Sub-sections:** Users (`#/settings/users-roles/users`) | Roles (`#/settings/users-roles/roles`)  
**Audit Date:** 2026-05-15

---

## Screenshots

- `screenshots/102-users-list.png` — Users list (single admin user)
- `screenshots/102-invite-user-roles-dropdown.png` — Invite User form with role dropdown
- `screenshots/102-roles-list.png` — Roles list (3 roles)
- `screenshots/102-admin-role-permissions.png` — Admin role permission matrix
- `screenshots/102-new-role-form.png` — New Role creation form

---

## Users Page

**URL:** `#/settings/users-roles/users`

### Table Columns

| Column | Description | Sortable |
|--------|-------------|---------|
| USER DETAILS | Avatar, Username, Email address | Yes |
| ROLE | Assigned role name | Yes |
| STATUS | Active / Invited / Deactivated | No |
| MORE ACTIONS | Kebab dropdown | No |

### Current Users (lerno org)

| User | Email | Role | Status |
|------|-------|------|--------|
| abhijithss2255 | abhijithss2255@gmail.com | Admin | Active |

### Actions

| Action | Description |
|--------|-------------|
| Invite User | Opens invite form at `#/settings/users-roles/invite` |
| More Actions dropdown | Per-user actions: Edit, Deactivate, Delete (inferred) |
| User name link | Opens edit user form at `#/settings/users-roles/{userId}/edit` |

### Invite User Form

**URL:** `#/settings/users-roles/invite`

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Name | Text | Yes | Display name |
| Email | Email | Yes | Invitation sent to this address |
| Role | Dropdown | Yes | Admin \| Manager \| Reimbursements and POI Reviewer |

**Actions:** Invite User | Cancel  
**Preview invitation template** link — shows preview of invitation email content

---

## Roles Page

**URL:** `#/settings/users-roles/roles`

### System Roles (3 built-in)

| Role | Description |
|------|-------------|
| Admin | Unrestricted access to all modules. |
| Manager | Access to all modules except organisation settings. |
| Reimbursements and POI Reviewer | View and approve reimbursements and proof of investments. |

### Actions

| Action | Description |
|--------|-------------|
| New Role | Creates custom role at `#/settings/users-roles/roles/new` |
| Role Name button | Opens role view at `#/settings/users-roles/roles/{id}/view` |
| More Actions dropdown | Per-role: Edit, Delete (inferred) |

---

## Permission Categories (Role Matrix)

The role permission matrix is a checkbox grid with the following structure:

### Employee Module Permissions

| Sub-Module | Permissions Available |
|-----------|----------------------|
| Basic And Personal Details | FULL ACCESS \| VIEW \| CREATE \| EDIT \| DELETE |
| Salary Details | FULL ACCESS \| VIEW \| CREATE \| EDIT \| DELETE |
| Payment Information | FULL ACCESS \| VIEW \| CREATE \| EDIT \| DELETE |
| Salary Revision | FULL ACCESS \| VIEW \| CREATE \| EDIT \| DELETE |
| Leave | FULL ACCESS \| VIEW \| CREATE \| EDIT \| DELETE |
| Attendance | FULL ACCESS \| VIEW \| CREATE \| EDIT \| DELETE |
| Perquisites | FULL ACCESS \| VIEW \| CREATE \| EDIT \| DELETE |
| Loan Summary | FULL ACCESS \| VIEW \| CREATE \| EDIT \| DELETE |
| Reimbursement Summary | FULL ACCESS \| VIEW \| CREATE \| EDIT \| DELETE |
| Declarations | FULL ACCESS \| VIEW \| CREATE \| EDIT \| DELETE |

**Standalone toggles (Employee section):**
- "Provide access to view and download employee Payslips, TDS worksheets and Forms."
- "Provide access to terminate an employee from the organisation."
- "Provide access to delete an employee who isn't associated to any Payrun."

### Payroll Run Permissions

| Sub-Module | Permissions Available |
|-----------|----------------------|
| Payroll Run | FULL ACCESS \| VIEW \| CREATE \| EDIT \| APPROVE \| PAY |

### Loan Permissions

| Sub-Module | Permissions Available |
|-----------|----------------------|
| Loan | FULL ACCESS \| VIEW \| CREATE \| EDIT \| APPROVE \| RECORD DISBURSEMENT |

### Approvals Permissions

| Sub-Module | Permissions Available |
|-----------|----------------------|
| Reimbursements | FULL ACCESS \| VIEW \| CREATE \| EDIT \| DELETE \| APPROVE |

**Standalone toggles (Approvals section):**
- "Provide access to approve Proof Of Investments."
- "Provide access to approve Salary Revisions."
- "Provide access to approve Leave Requests."
- "Provide access to approve Attendance Regularization requests."

### Settings Permissions (Checkboxes per item)

| Setting | Access Toggle |
|---------|--------------|
| Update organization profile | Checkbox |
| Work Location | Checkbox |
| Department | Checkbox |
| Designation | Checkbox |
| Manage Users | Checkbox |
| Taxes | Checkbox |
| Pay Schedule | Checkbox |
| Salary and Statutory Components | Checkbox |
| Email Templates | Checkbox |
| Salary Templates | Checkbox |
| Reporting Tags | Checkbox |
| Automation | Checkbox |
| Custom Fields | Checkbox |
| Holidays | Checkbox |
| Leave | Checkbox |
| Attendance | Checkbox |
| Provide access to protected data | Checkbox |
| Integration | Checkbox |
| Incoming Webhook | Checkbox |

### Preferences Permissions

| Setting | Access Toggle |
|---------|--------------|
| Reimbursement and FBP Settings | Checkbox |
| IT Declaration Settings | Checkbox |
| Approval Preferences | Checkbox |

### Report Permissions

| Report Type | Access Toggle |
|------------|--------------|
| Payroll Reports | Checkbox |
| Statutory Reports | Checkbox |
| Declaration Reports | Checkbox |
| Deduction Reports | Checkbox |
| Tax Reports | Checkbox |
| Loan Reports | Checkbox |
| Leave Reports | Checkbox |
| Attendance Reports | Checkbox |
| Activity Logs | Checkbox |

### Documents Permissions

| Permission | Access Toggle |
|-----------|--------------|
| View Documents | Checkbox |
| Upload Documents | Checkbox |
| Delete Documents | Checkbox |
| Manage Folder | Checkbox |

### Taxes And Forms Permissions

| Permission | Access Toggle |
|-----------|--------------|
| View Forms & Taxes | Checkbox |
| Record Tax Payment | Checkbox |
| Delete Tax Payment | Checkbox |
| Record and Undo Filing | Checkbox |

---

## Role Permission Matrix (Observed States)

Based on checkbox states observed in Manager role view (all disabled/read-only in view mode):

### Manager Role Key Observations
- **Leave and Attendance:** unchecked (Manager does NOT have leave/attendance access by default)
- **Declarations:** unchecked
- **Settings:** Manager does NOT have "Update organization profile", "Manage Users", "Taxes", "Pay Schedule", "Salary and Statutory Components" — consistent with the description "except organisation settings"
- All other employee, payroll, loan, approvals permissions appear granted

### Admin Role
- All permissions granted (all checkboxes checked)
- Matches description: "Unrestricted access to all modules"

### Reimbursements and POI Reviewer Role
- Narrow permission set: only Reimbursements approval + POI approval
- Not observed directly — inferred from role description

---

## New Role Creation

**URL:** `#/settings/users-roles/roles/new`

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Role Name | Text | Yes | Unique name |
| Description | Textarea | No | Role description |
| Permission Categories | Checkbox matrix | No | All permission categories as described above |

**"Permission Categories" header:** "Choose what this role can access or manage."

All permission checkboxes default to unchecked for new roles.

**Actions:** Save | Cancel

---

## Business Rules

1. **3 system roles** (Admin, Manager, Reimbursements and POI Reviewer) are pre-built and cannot be deleted.
2. **Custom roles** can be created via "New Role" — org-level, available to all users in that org.
3. **One role per user** — each user has exactly one role assignment.
4. **Admin = unrestricted** — can access all settings including user management.
5. **Manager = restricted settings** — cannot modify org settings (profile, users, taxes, pay schedule, statutory components).
6. **Payroll Run permissions** include a separate PAY action (beyond Approve) — required for the "Record Payment" step.
7. **Loan permissions** include RECORD DISBURSEMENT separately from CREATE — maps to recording manual disbursement events.
8. **Protected data access** is a single toggle — controls access to sensitive fields (PAN, Aadhaar, bank details). Must be explicitly granted.
9. **Leave and Attendance** permissions are present but likely no-ops unless Leave & Attendance module is enabled.
10. **Invitation flow:** Email invitation sent to new user; user must accept to activate their account.
11. **User can only be in one org at a time** (per Zoho Payroll's org model — not multi-org per user account in base product).

---

## Data Relationships

- User → Role (many-to-one): each user has one role
- Role → Org (many-to-one): roles belong to one org; Admin/Manager/Reviewer are system roles replicated per org
- Role → Permission set (one-to-many): role has many granular permissions
- User → Org (many-to-one): user belongs to one organisation

---

## Open Questions

- [ ] Can a Manager role be customised (e.g., grant Taxes access but keep other restrictions)?
- [ ] What happens to a user's data access if their role is changed mid-session?
- [ ] Can the same email be used for multiple organisations in Zoho Payroll?
- [ ] Is there an "Employee Self Service" role for employees to access the portal?
- [ ] What does "Provide access to protected data" specifically reveal? PAN + Aadhaar + Bank account?
- [ ] Can individual employees be assigned to specific managers (employee-manager hierarchy beyond role-level)?
- [ ] Is there a row-level security concept (manager can only see employees in their department)?
- [ ] What is the "Show dropdown menu" action per user — does it include Deactivate, Resend Invite, Change Role?
