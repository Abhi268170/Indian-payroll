# UF-84: Employee Portal — Overview and Configuration

**Module:** Settings > Employee Portal / Employee Self-Service
**Tested:** 2026-05-16
**Mock Data Used:** Admin account (abhijithss2255); Portal access enabled
**App State Before:** Navigated to `#/settings/portal/preferences`

## Steps Executed
1. Navigate to `#/settings/portal/preferences`
2. Observe Employee Portal settings and configuration options
3. Navigate to Dashboard to find mobile app portal links
4. Document portal access mechanism

---

## Employee Portal — Nature and Access

### Portal Type
The Zoho Payroll employee portal is **mobile-first**: available as a dedicated mobile app.

**Mobile App:**
- iOS App: Apple App Store — "Employee Portal - Zoho Payroll" (ID: 1450810850)
- Android App: Google Play Store — `com.zoho.payroll`
- App Store URL: `https://itunes.apple.com/in/app/employee-portal-zoho-payroll/id1450810850`
- Play Store URL: `https://play.google.com/store/apps/details?id=com.zoho.payroll`

**Web Portal:**
- Employees may also have web access via `payroll.zoho.in` with employee credentials
- Exact employee web URL not confirmed — admin and employee share the same domain but different roles

---

## Employee Portal Settings (`#/settings/portal/preferences`)

### Navigation within Settings
Settings left panel > "Employee Portal" section > sub-items:
- Preferences (current)
- Web Tabs

### Form: Employee Portal Preferences

| Field | Type | Value/State |
|-------|------|------------|
| Enable Portal Access | Toggle | Active (enabled) |
| Banner Message | Text area (free text) | Empty |
| Banner Display Until | Date picker (dd/MM/yyyy) | Empty |
| Portal Contact Information | Display (email) | abhijithss2255@gmail.com |
| Show documents in employee portal | Checkbox | Unchecked |

### Field Details

**Enable Portal Access (Toggle)**
Label: "Enable Portal Access — Active"
Description: "The employee portal allows your employees to access their salary information and perform payroll related activities like declaring their Flexible Benefit Plan (FBP) and submitting investment proofs for approval."

**Banner Message**
- Used to send important notifications or announcements to employees
- Displayed at the top of the Home page in the Employee Self-service Portal
- Has a "View Sample Preview" button — shows what the banner looks like to employees
- Paired with an expiry date field (message displays until that date)

**Portal Contact Information**
- Email address to which employees can send queries through the portal
- Current contact: abhijithss2255@gmail.com
- "Manage Contacts" link → `#/settings/portal/preferences?configure_emails=true`

**Show documents in employee portal (Checkbox)**
- Description: "Enable this option to make documents visible for employees to access in the employee portal."
- Currently unchecked — employees cannot see org/personal documents in portal
- Documents module content only visible to employees if this is enabled

### Form Actions
| Button | Action |
|--------|--------|
| Save | Saves portal preference settings |

---

## Employee Portal — What Employees Can Access

Based on the portal description and domain knowledge:

| Feature | Available to Employee | Admin Controls |
|---------|----------------------|----------------|
| Payslip download | Yes | Always available |
| TDS Sheet / Tax Summary | Yes | Always available |
| Form 16 (after publish) | Yes | Admin must publish |
| IT Declaration submission | Yes | Admin must Release IT Declaration |
| FBP Declaration | Yes | Admin must enable FBP |
| POI (Proof of Investment) upload | Yes | Admin must enable |
| Reimbursement claim submission | Yes | Always available |
| Document access | Configurable | "Show documents" checkbox |
| Salary Structure view | Yes | Typically read-only |
| Bank Details update | Configurable | May require admin approval |

---

## IT Declaration Portal Access

The key control is in `#/settings/preferences/it-declaration`:
- **IT Declaration is currently LOCKED** — employees cannot submit declarations via portal
- Admin must click "Release IT Declaration" to unlock
- Once released: employees can log into portal, submit investment declarations, upload POI

**Consequence of current state:** Since IT Declaration is locked, no employee can declare investments. This causes TDS computation to be zero (no exemptions claimed) — which is actually MORE conservative (higher TDS, not lower), but since salaries are below taxable limits or not yet declared, TDS = ₹0 for all.

---

## IT Declaration Preferences Configuration

URL: `#/settings/preferences/it-declaration`

| Setting | State |
|---------|-------|
| IT Declaration is Locked (org-wide) | YES — not released |
| Allow employees to switch tax regimes | CHECKED (enabled) |
| Allow TDS modification to exceed calculated tax amount | NOT checked |

**"Allow employees to switch tax regimes" = CHECKED** means employees with new regime can switch to old regime (or vice versa) through the portal. This is significant: despite the system being "new regime only" in V1 build intent, Zoho Payroll as a product allows old regime switching.

---

## Settings Navigation Structure (Complete)

From `#/settings/preferences/it-declaration` navigation panel:

### Group 1: Organisation Settings
| Section | Sub-items | URLs |
|---------|-----------|------|
| Organisation | (not expanded) | — |
| Users and Roles | (not expanded) | — |
| Taxes | (not expanded) | `#/settings/taxes` |
| Setup & Configurations | Pay Schedule, Statutory Components, Salary Components, Employee Portal, Claims and Declarations | Multiple |
| Customisations | (not expanded) | — |
| Automations | (not expanded) | — |

### Group 2: Module Settings
| Section | Sub-items | URLs |
|---------|-----------|------|
| General | (not expanded) | — |
| Payments | (not expanded) | — |

### Group 3: Extensions & Developer Data
| Section | Sub-items | URLs |
|---------|-----------|------|
| Integrations | (not expanded) | — |
| Developer Data | (not expanded) | — |

---

## Dashboard Onboarding Checklist (5/7 Completed)

From `#/home/dashboard`:
| Step | Status | URL |
|------|--------|-----|
| 1. Add Organisation Details | Completed | `#/settings/orgprofile` |
| 2. Provide your Tax Details | Completed | `#/settings/taxes` |
| 3. Configure your Pay Schedule | Completed | `#/settings/pay-schedules` |
| 4. Set up Statutory Components | NOT Completed | `#/settings/statutory-details/list` |
| 5. Set up Salary Components | NOT Completed — "Complete Now" button | `#/settings/salary-components/earnings` |
| 6. Add Employees | Completed | `#/people/employees` |
| 7. Configure Prior Payroll | Completed (shown) | `#/prior-payroll` |

**Step 4 not completed:** Statutory components (EPF/ESI/LWF/PT) need configuration — explains why all employees have statutory components disabled.

**Step 5 not completed:** Salary Components setup — explains why salary structure may be incomplete.

**Step 7 "Completed" is misleading:** Prior Payroll page shows "Enable Prior Payroll" button — it was NOT enabled. The onboarding marks it complete as soon as the admin visits the page, not based on actual configuration.

---

## Prior Payroll Page (`#/prior-payroll`)

### Content
Message: "You have not checked the option to include prior payroll during setup. In case you need to add prior payroll data for your employees, you can import the necessary details and continue processing payrolls."

| Button | Action |
|--------|--------|
| Enable Prior Payroll | Enables import of prior payroll YTD data |
| Close | Closes the page |

**Impact of not enabling Prior Payroll:**
- Employees who joined before the first Zoho pay run have no YTD data from prior employer
- TDS computation for the full FY may be incorrect if prior-employer salary is not captured
- Form 16 Part B will only show Zoho-computed salary (not total FY salary)

This is particularly important for employees who joined mid-year and had salary at another company before.

---

## Giving Module (`#/donations`)

### Layout
- Active Campaigns tab with "New" button
- Empty state: "There are no active campaigns"
- "Create campaigns to allow your employees to contribute for a cause" — tagline
- "New Campaign" button

### Purpose
Allows the employer to create charitable donation campaigns. Employees can contribute amounts through their payroll (deducted from net pay). Donations are eligible for Section 80G deduction.

### Empty State
No active campaigns in demo org.

---

## Documents Module (`#/documents/folder`)

### Layout
- Left panel: Folder navigation
- Right panel: "All Documents" view with drag-and-drop upload

### Folder Structure
| Section | Folders |
|---------|---------|
| Org Folder | Offer Letters |
| Employee Folder | Personal Documents |

### Storage Limit
"1GB / 100 employees" — displayed in left sidebar.

### All Documents View
**Filters:**
- Select Employee Status (dropdown)
- Select an Employee (searchable combobox)

**Upload:**
- Drag & Drop zone
- "Choose File" button
- Note: When uploading .zip, files inside must be .pdf; file name must correspond to employee ID (for employee folder auto-assignment)
- File size limit: 50MB per file

### Navigation
| Link | URL |
|------|-----|
| All Documents | `#/documents/folder` |
| Offer Letters | `#/documents/folder?folder_id=...&folder_name=Offer+Letters` |
| Personal Documents | `#/documents/folder?folder_id=...&folder_name=Personal+Documents` |
| Trash | `#/documents/trash` |

### Folder Types
| Type | Access |
|------|--------|
| Org Folder | Admin-managed; can be shared with employees |
| Employee Folder | Per-employee documents; employee can view via portal if "Show documents" enabled |

---

## Gaps / Observations
- Employee portal web URL not confirmed — only mobile app links found
- IT Declaration is locked — prevents employees from using the portal for declarations
- Step 4 and 5 of onboarding not completed — statutory and salary components configuration incomplete
- Prior Payroll marked "Completed" in dashboard but actually not enabled — onboarding checklist is misleading
- Documents module "Show documents in employee portal" is unchecked — employees cannot see documents even if portal is active
- Giving module has no active campaigns

## Open Questions
- [ ] Is there a web URL for the employee self-service portal (not mobile)?
- [ ] What does "Web Tabs" (`#/settings/portal/webtabs`) configure?
- [ ] Can employees update their own bank details through the portal, or is that admin-only?
- [ ] Does the employee portal require separate Zoho credentials (invite email) or does it share admin credentials?
- [ ] Prior Payroll: What fields are captured in the YTD import? (Basic, HRA, TDS deducted, PF deducted?)
