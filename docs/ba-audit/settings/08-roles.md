# Settings > Roles

## URL
`#/settings/users-roles/roles`

Sub-routes:
- `#/settings/users-roles/roles/{id}/view` — View/Edit role permissions
- `#/settings/users-roles/roles/new` — New custom role (inferred from "New Role" button)

## Purpose
Defines permission roles that control what each user can view and do within Zoho Payroll. Three system-defined roles exist by default; custom roles can be created via subscription entitlement.

## Page Layout
Table: Role Name | Description | More Actions.
Header: "Roles" heading + "New Role" button (icon + text).

## System-Defined Roles (3 built-in, not deletable)

| Role | Description |
|------|-------------|
| **Admin** | Unrestricted access to all modules. |
| **Manager** | Access to all modules except organisation settings. |
| **Reimbursements and POI Reviewer** | View and approve reimbursements and proof of investments. |

## Permission Matrix (observed from Admin and Manager role views)

The role detail page (`/view`) shows a comprehensive RBAC permission matrix. All categories and permission types discovered:

### Employee Module
| Sub-module | Permission Types |
|-----------|-----------------|
| Basic And Personal Details | Full Access, View, Create, Edit, Delete |
| Salary Details | Full Access, View, Create, Edit, Delete |
| Payment Information | Full Access, View, Create, Edit, Delete |
| Salary Revision | Full Access, View, Create, Edit, Delete |
| Leave | Full Access, View, Create, Edit, Delete |
| Attendance | Full Access, View, Create, Edit, Delete |
| Perquisites | Full Access, View, Create, Edit, Delete |
| Loan Summary | Full Access, View, Create, Edit, Delete |
| Reimbursement Summary | Full Access, View, Create, Edit, Delete |
| Declarations | Full Access, View, Create, Edit, Delete |

**Additional Employee toggles (boolean, not matrix):**
- Provide access to view and download employee Payslips, TDS worksheets and Forms
- Provide access to terminate an employee from the organisation
- Provide access to delete an employee who isn't associated to any Payrun

### Payroll Run Module
| Sub-module | Permission Types |
|-----------|-----------------|
| Payroll Run | Full Access, View, Create, Edit, Approve, **Pay** |

### Loan Module
| Sub-module | Permission Types |
|-----------|-----------------|
| Loan | Full Access, View, Create, Edit, Approve, **Record Disbursement** |

### Approvals Module
| Sub-module | Permission Types |
|-----------|-----------------|
| Reimbursements | Full Access, View, Create, Edit, Delete, Approve |

**Additional Approvals toggles:**
- Provide access to approve Proof Of Investments
- Provide access to approve Salary Revisions
- Provide access to approve Leave Requests
- Provide access to approve Attendance Regularization requests

### Settings Permissions (boolean toggles — grant/deny access to each settings page)
- Update organization profile
- Work Location
- Department
- Designation
- Manage Users
- Taxes
- Pay Schedule
- Salary and Statutory Components
- Email Templates
- Salary Templates
- Reporting Tags
- Automation
- Custom Fields
- Holidays
- Leave
- Attendance
- Provide access to protected data (PAN/Aadhaar/bank — sensitive fields)
- Integration
- Incoming Webhook
- Preferences
- Reimbursement and FBP Settings
- IT Declaration Settings
- Approval Preferences

### Report Permissions (boolean toggles)
- Payroll Reports
- Statutory Reports
- Declaration Reports
- Deduction Reports
- Tax Reports
- Loan Reports
- Leave Reports
- Attendance Reports
- Activity Logs

### Documents Permissions
- View Documents
- Upload Documents
- Delete Documents
- Manage Folder

## Buttons & Actions

### Roles List
| Button | Label | State | Action |
|--------|-------|-------|--------|
| New Role | "New Role" | Always enabled | Opens New Role creation form (requires Custom Roles subscription feature) |
| Role Name (button) | Role name | Always enabled | Navigates to role detail/permission view: `#/settings/users-roles/roles/{id}/view` |
| More Actions (system roles) | Three-dot | Enabled | Expected: Edit, Clone, Delete (system roles may have Edit/Clone but not Delete) |

### Role Detail View
| Button | Label | State | Notes |
|--------|-------|-------|-------|
| Back / View Roles | "View Roles" | Always enabled | Returns to roles list |
| Save / Update | (expected) | Enabled when changes made | Saves permission changes for custom roles; system roles may be read-only |

## Tabs (if any)
None.

## Conditional Logic
1. **System roles** (Admin, Manager, Reimbursements and POI Reviewer) — cannot be deleted. May have limited editability.
2. **New Role button** — requires "Custom Roles" subscription feature (listed in Subscriptions page). On free trial, it is accessible.
3. **"Provide access to protected data"** — a special permission toggle that controls access to encrypted sensitive fields (PAN, Aadhaar, bank account). This is a critical security gate.
4. **Full Access checkbox** — checking "Full Access" for a module likely auto-checks all sub-permissions (View, Create, Edit, Delete, Approve).

## Cross-Module Impact
| Setting | Impacts |
|---------|---------|
| Role permissions | Every page in the application enforces role-based access. Determines visibility of buttons, actions, and data |
| "Protected data" permission | Controls whether a user can see unmasked PAN, Aadhaar, and bank account numbers |
| "Manage Users" permission | Controls access to this Users & Roles settings section |
| "Pay" permission on Payroll Run | Only users with this permission can execute the final payment step in a pay run |
| "Approve" permissions | Feed into the approval workflow chain for payroll, loans, reimbursements, leave |

## Observations & Notes
1. **"Pay" as a distinct permission** on Payroll Run is notable — this separates the role of approving a payroll (reviewing it) from actually initiating the bank transfer/payment. Strong four-eyes-principle design.
2. **"Record Disbursement" on Loan** — similarly separates loan approval from actual disbursement recording. Correct for financial controls.
3. **"Provide access to protected data"** — this toggle is critical for GDPR/PDPB compliance in India. Without it, PAN and Aadhaar remain masked for that user.
4. **No field-level permissions** within a sub-module — e.g., you can't give View on salary details but hide the HRA component. Access is at the sub-module level only.
5. **No time-bound or condition-based permissions** — RBAC is static; no attribute-based access control (ABAC).
6. **"Reimbursements and POI Reviewer" role** is very narrow — designed specifically for the person who reviews investment declaration proofs during the December-January POI submission window. This is a real-world Indian payroll workflow requirement.
7. For our build: We need at minimum these three roles. The permission matrix structure (module → sub-module → CRUD + special actions) is a solid RBAC model to adopt.

## Screenshots
- `docs/ba-audit/settings/screenshots/08-roles.png` — roles list
- `docs/ba-audit/settings/screenshots/08-roles-admin-permissions.png` — Admin role permissions matrix
