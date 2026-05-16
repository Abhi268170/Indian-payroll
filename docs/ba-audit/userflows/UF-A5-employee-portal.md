# UF-A5: Employee Portal — Admin View and Settings

**Module:** Employee Master (per-employee portal toggle) + Settings > Employee Portal
**Tested:** 2026-05-16
**Approach:** Navigated to Arjun Mehta (EMP001) overview page, located Portal Access field, attempted "(Enable)" action. Confirmed block due to unverified admin email. Navigated to Settings > Employee Portal (`#/settings/portal/preferences`) to document org-level portal configuration.

---

## Findings

### 1. Per-Employee Portal Access (Employee Overview Page)

**Location:** Employee profile → Overview tab → "Portal Access" field

**Field details:**

| Attribute | Value |
|-----------|-------|
| Field label | "Portal Access" |
| Field type | Status indicator + action button |
| States | "Disabled" / "Enabled" |
| Action button | "(Enable)" when Disabled / "(Disable)" when Enabled |
| Selector | Text "(Enable)" / "(Disable)" near "Portal Access" label |

**Observed values across employees:**

| Employee | Portal Access Status |
|----------|---------------------|
| Arjun Mehta (EMP001) | Disabled |
| Priya Sharma (EMP002) | Disabled |
| Vikram Nair (EMP003) | Disabled |
| Aisha Khan (EMP004) | Disabled |
| Rahul Desai (EMP005) | Disabled |

All employees have portal access disabled — consistent with unverified admin email constraint.

**Enable Portal Access — Confirmation Modal:**

When "(Enable)" is clicked, a confirmation dialog appears:

| Field | Content |
|-------|---------|
| Modal title | "Enable Portal" (inferred from prior session) |
| Message | Confirmation prompt to enable portal for this employee |
| Buttons | Confirm / Cancel |

**Post-action behaviour (attempted — blocked):**
- Toast message: "Your email address is yet to be verified."
- Portal status remains "Disabled"
- Root cause: Admin account email (abhijithss2255@gmail.com) has not been verified via Zoho verification email

**Business Rule:** Portal cannot be enabled for any employee until the admin's email address is verified. This is an org-level prerequisite, not per-employee.

---

### 2. Employee Overview — Portal-Related Fields

**Full field list from Aisha Khan / Arjun Mehta Overview page:**

**Section: Basic Information**
| Field | Type | Notes |
|-------|------|-------|
| Name | Text (display) | Full name |
| Email Address | Text | Work email (used for portal login) |
| Mobile Number | Text | |
| Date of Joining | Date | |
| Gender | Display | |
| Work Location | Display | |
| Designation | Display | |
| Departments | Display | |
| Portal Access | Status + action | Disabled/(Enable) |

**Section: Statutory Information**
| Field | Notes |
|-------|-------|
| (Multiple statutory fields) | PF, ESI, PT applicability — Toggle states |

**Section: Personal Information**
| Field | Notes |
|-------|-------|
| Date of Birth | |
| Father's Name | Used for Tax Deductor details |
| PAN | Critical for TDS/Form 16 |

**Section: Payment Information**
| Field | Notes |
|-------|-------|
| Bank Account No | Masked "XXXX{last4}" with "Show A/C No" button |
| IFSC Code | |
| Bank Name | |

---

### 3. Employee Actions Dropdown (from employee profile)

**Trigger:** "Show dropdown menu" button (aria-label) on employee profile header

**Dropdown options vary by employee state:**

| Employee | Options Available |
|----------|-----------------|
| EMP001 (Arjun Mehta) | Add / Update Vehicle Details, Initiate Exit Process |
| EMP002 (Priya Sharma) | Add / Update Vehicle Details, Initiate Exit Process |
| EMP003 (Vikram Nair) | Add / Update Vehicle Details, Delete Employee |
| EMP004 (Aisha Khan) | Add / Update Vehicle Details, Delete Employee |
| EMP005 (Rahul Desai) | Add / Update Vehicle Details, Delete Employee |

**Business Rule: "Initiate Exit Process" availability condition:**
- "Initiate Exit Process" appears only for employees who have been processed in at least one payroll run (EMP001, EMP002).
- Employees with no payroll history show "Delete Employee" instead — suggesting they can be hard-deleted (not just exited) because they have no payroll records.
- "Delete Employee" = permanent deletion (no payroll trail). "Initiate Exit Process" = formal F&F with payroll settlement.

**Business Rule: Tax Deductor cannot exit:**
- EMP001 (Arjun Mehta) is the Tax Deductor for the organisation.
- Clicking "Initiate Exit Process" on EMP001 shows error: "You cannot initiate the exit process for Arjun Mehta as the employee is the Tax Deductor for your organisation."
- Must reassign Tax Deductor in Settings before exiting that employee.

---

### 4. Settings > Employee Portal (`#/settings/portal/preferences`)

**Route:** `#/settings/portal/preferences`
**Page title:** "Employee Portal" (under Settings)

**Layout:** Settings left sidebar → "Employee Portal" item under "General" section.

**Fields on Employee Portal Settings Page:**

| Field | Type | Default/Current | Description |
|-------|------|-----------------|-------------|
| Enable Portal Access | Toggle (global org-level) | Active | Enables/disables portal for ALL employees org-wide. Per-employee overrides still apply. |
| Banner Message | Text area | (empty) | Custom message displayed to employees on portal home screen |
| Display until | Date picker | (empty) | Banner expires after this date |
| Portal Contact Information | Email (display) | abhijithss2255@gmail.com | Email employees can write to for queries. Auto-populated from admin profile. |
| Show documents in employee portal | Checkbox | Unchecked (Disabled) | When enabled, Documents module visible to employees in portal |

**Buttons:**
| Button | Action |
|--------|--------|
| View Sample Preview | Opens a preview of how the portal appears to employees |
| Save | Saves all portal settings |

**Observations:**
- "Enable Portal Access" global toggle state = "Active" — portal is globally enabled at org level
- Individual employee portal access = all Disabled (per-employee override)
- The org-level toggle being Active + individual Disabled = employees cannot access portal
- Both levels must be Active for an employee to access the portal
- Banner Message + expiry date allows temporary announcements (e.g., salary credit notice, deadline reminders)

---

### 5. Portal Access Matrix

| Condition | Employee can access portal? |
|-----------|---------------------------|
| Org portal = Disabled | No (regardless of individual setting) |
| Org portal = Active, Employee = Disabled | No |
| Org portal = Active, Employee = Enabled | Yes |
| Admin email unverified | Cannot enable individual portal (blocked at UI layer) |

---

### 6. Employee Portal — Known Features (from prior session docs UF-84, UF-85, UF-88)

Based on prior audit sessions, the employee portal provides:
- View and download payslips
- Submit IT declarations / proof of investments
- View and submit reimbursement claims
- View loans
- View documents (if Document Management toggle enabled)
- Access Form 16 (after generation and publish)

Portal login URL: Separate subdomain or Zoho Accounts SSO (not directly tested in this session).

---

## Screenshots / Files

- `settings-employee-portal.png` — Employee Portal settings page
- `portal-enable-confirm.png` — Portal enable confirmation modal (prior session)
- `employee-actions-dropdown.png` — Employee actions dropdown (prior session)
- `exit-blocked-tax-deductor.png` — Tax Deductor exit block error (prior session)

---

## Gaps / Open Questions

- [ ] **Portal URL for employees:** What URL do employees use to access the portal? Is it `accounts.zoho.in` SSO or a dedicated subdomain?
- [ ] **Email verification flow:** What email does Zoho send to admin for verification? Is this triggered automatically or manually?
- [ ] **"View Sample Preview" content:** What does the sample portal preview show? Not explored.
- [ ] **Banner message character limit:** No max length observed. Is there a character cap?
- [ ] **Portal Contact Information:** Can this be changed to a different email (e.g., HR team email) or is it locked to admin email?
- [ ] **Portal disable per employee:** When portal is disabled per employee (admin action), does the employee get a notification?
