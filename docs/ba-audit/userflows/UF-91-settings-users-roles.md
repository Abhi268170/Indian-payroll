# UF-91: Settings — Users and Roles

**Module:** Settings > Organisation Settings > Users and Roles
**Tested:** 2026-05-16
**URLs:**
- Users: `#/settings/users-roles/users`
- Roles: `#/settings/users-roles/roles`

---

## Users Page

### Page Layout
- Heading: "Users"
- "Invite User" button (primary action)
- Table with columns: User Details, Role, Status, More Actions

### Current Users (Demo Org)
| User Details | Role | Status |
|-------------|------|--------|
| A — abhijithss2255 (abhijithss2255@gmail.com) | Admin | Active |

### Table Columns
| Column | Content |
|--------|---------|
| User Details | Avatar initial, Display Name, Email address |
| Role | Role name (Admin / Manager / Custom) |
| Status | Active / Invited / Inactive |
| More Actions | Dropdown (Edit, Deactivate, Remove) |

### User Profile Link
Clicking username: `#/settings/users-roles/{userId}/edit`
User ID observed: `3848927000000032001`

### "Invite User" Flow (Expected)
1. Click "Invite User"
2. Enter email address
3. Select role: Admin / Manager / Reimbursements and POI Reviewer / Custom
4. Send invitation
5. Invited user receives email with activation link
6. User status: "Invited" until accepted

---

## Roles Page

### Page Layout
- Heading: "Roles"
- "New Role" button (+ icon)
- Table with columns: Role Name, Description, More Actions

### System-Defined Roles (3 roles — confirmed)

| Role Name | Description | Customizable |
|-----------|-------------|--------------|
| Admin | Unrestricted access to all modules. | No (system) |
| Manager | Access to all modules except organisation settings. | No (system) |
| Reimbursements and POI Reviewer | View and approve reimbursements and proof of investments. | No (system) |

### Role Actions (Dropdown per role)
- Edit (for custom roles — system roles may be view-only)
- Delete (for custom roles only)

### "New Role" — Custom Role Creation
Allows creating a custom role with specific module-level permissions.

**Expected role permissions matrix:**
| Module | Permission Types |
|--------|----------------|
| Dashboard | View |
| Employees | View / Create / Edit / Delete |
| Pay Runs | View / Process / Approve / Finalize |
| Approvals | View / Approve / Reject |
| Taxes & Forms | View / Generate |
| Loans | View / Create / Edit |
| Reports | View / Export |
| Settings | View / Edit (Organisation) |

---

## RBAC Model

### Access Control Type
**Role-Based Access Control (RBAC)** — role determines module access.
Each user has exactly one role.

### Role Hierarchy (Inferred)
```
Admin (full access)
  └── Manager (all modules except org settings)
        └── Reimbursements and POI Reviewer (restricted: only approvals)
              └── Custom Roles (configurable)
```

### Multi-User Scenario
Multiple users can share the same role.
There is no per-user module permission (only per-role).

---

## Key RBAC Observations

### Admin Role
- Unrestricted: Can access Settings (Organisation Profile, Users, Tax Details, all configurations)
- Can finalize pay runs
- Can manage users and roles
- Can configure statutory details

### Manager Role
- Cannot access: Settings > Organisation Settings (no org profile, no Tax Details, no Users)
- Can: Process pay runs, approve workflows, access reports
- Use case: Payroll manager who prepares but doesn't configure

### Reimbursements and POI Reviewer Role
- Very restricted: Only Approvals (Reimbursements + POI)
- Cannot view employee details, pay runs, reports, settings
- Use case: Department head who only approves reimbursements

---

## User Management Business Rules
1. Only Admin role can invite/remove users and change roles
2. System roles (Admin, Manager, Reimbursements and POI Reviewer) cannot be deleted
3. Each user has exactly one role (no multi-role per user)
4. Invited users cannot access until they accept the invitation
5. Deactivated users: Cannot login; data preserved
6. Removed users: Removed from org; may be re-invited

## Gaps / Observations
- Custom role permission matrix not observed (did not create a new role)
- Whether "Manager" role can finalize pay runs not confirmed from permissions screen
- User audit trail (who last changed a role) not explored

## Open Questions
- [ ] Can multiple users have the same Admin role (shared admin access)?
- [ ] Is there a record of who changed user roles and when (audit trail)?
- [ ] Can "Reimbursements and POI Reviewer" role access the Approvals > Salary Revision page?
- [ ] Is there a per-employee data restriction possible (e.g., Manager A sees only their team)?
