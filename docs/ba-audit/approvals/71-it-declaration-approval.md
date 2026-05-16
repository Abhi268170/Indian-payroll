# Approvals > IT Declaration & Proof Of Investments (Item 71)

## URL / Navigation Path

- POI Approvals List: `#/approvals/proof-of-investment`
- POI Unsubmitted Employees: `#/approvals/proof-of-investment/unsubmitted-list?filter_by=&page=1&per_page=50&type=unsubmit`
- IT Declaration Settings: `#/settings/preferences/it-declaration`
- POI Settings: `#/settings/preferences/proof-of-investment`

## Purpose

Enables admin to:
1. Review and approve/reject employee-submitted IT declarations
2. Review and approve/reject employee-submitted proof of investment (POI) documents
3. Monitor which employees have not submitted POI/declarations
4. Control the submission window (release/lock)

---

## POI Approvals List Page

**URL:** `#/approvals/proof-of-investment`
**Page Title:** "Approvals | POI | Zoho Payroll"

### Layout

```
[Header]
  ["All Investments" view dropdown]    [Warning badge: "N employee(s) yet to submit POI" View]
                                       [...Release POI] [Filter] [Help]
[Filter Band]
  [Fiscal Year: 2026-2027 dropdown] [Tax Regime: dropdown] [Employees: dropdown]
[Content]
  [Empty state with workflow diagram] OR [Table of POI submissions]
```

### View Toggle Dropdown Options

| View | Meaning |
|------|---------|
| All Investments | All employees with any POI status |
| Approval Pending Investments | Submitted but not yet approved by admin |
| Yet To Confirm Investments | Submitted but admin needs to confirm amounts |
| Approved Investments | Fully approved |

### Alert Badge: "2 employee(s) yet to submit POI"

- Appears in top-right of header as orange warning icon + text
- Clicking "View" navigates to: `#/approvals/proof-of-investment/unsubmitted-list`
- Shows which employees have not submitted their POI

### Filter Band Fields

| Field | Type | Default | Notes |
|-------|------|---------|-------|
| Fiscal Year | Dropdown | 2026 - 2027 | Shows available fiscal years |
| Tax Regime | Dropdown | "Select Tax Regime" | New Regime / Old Regime (old regime relevant here) |
| Employees | Autocomplete | "Select an Employee" | Filter to specific employee |

### Toolbar Actions

| Action | Trigger | Notes |
|--------|---------|-------|
| View dropdown | Dropdown | Status-based filter |
| "2 employee(s) yet to submit POI View" | Alert + link | Shows unsubmitted employees |
| Release POI | "..." menu item | Opens POI submission window for employees |
| Filter | Icon button | Toggle filter band |
| Instant Helper | "?" | In-app help |

### Empty State (Observed)

Illustrated diagram showing 3-step workflow:
1. Employee submits proof of investments
2. Payroll Admin approves or rejects the submitted proofs
3. Employee enjoys tax savings for the declared investment

Text: "This is your space to review your employees' investment proofs! Your employees can submit their investment proofs through the employee portal once you enable the option in **Settings > Preferences**."

---

## POI Unsubmitted Employees List

**URL:** `#/approvals/proof-of-investment/unsubmitted-list?filter_by=&page=1&per_page=50&type=unsubmit`

### Layout

```
[Header: "Employees Yet to Submit POI (N)"]
  [Info: "POI is locked. Release"] [Export] [Send Reminder]
[Table]
  [Checkbox] EMPLOYEE NAME | EMAIL ID | PORTAL STATUS | POI RELEASE STATUS
```

### Columns

| Column | Type | Values Observed |
|--------|------|-----------------|
| Checkbox | Bulk select | For selecting multiple employees |
| Employee Name | Text | "Arjun Mehta - EMP001", "Priya Sharma - EMP002" |
| Email ID | Text | arjun.mehta@lerno.com, priya.sharma@lerno.com |
| Portal Status | Status badge | "Disabled" (red) — employee portal not yet enabled |
| POI Release Status | Text | "-" (not released yet) |

### Data Observed

- EMP001 Arjun Mehta: Portal Disabled, POI not released
- EMP002 Priya Sharma: Portal Disabled, POI not released
- EMP003, EMP004, EMP005: Not in the list (either they submitted or are not subject to IT)

### Actions

| Action | Notes |
|--------|-------|
| "POI is locked. Release" | Opens POI release flow; same as Settings > POI > Release Proof Of Investments |
| Export | Downloads the unsubmitted employee list |
| Send Reminder | Sends email reminder to employees to submit POI |

---

## IT Declaration Settings

**URL:** `#/settings/preferences/it-declaration`
**Settings Path:** Settings > Setup & Configurations > Claims and Declarations > Income Tax Declaration tab

### Configuration Fields

| Field | Type | Current State | Notes |
|-------|------|---------------|-------|
| IT Declaration window status | Display | "IT Declaration is Locked" | Must be released for employees to submit |
| Release IT Declaration | Button | Active | Opens submission window |
| Allow employees to switch tax regimes | Checkbox | Observed (state unknown) | Lets employee choose New vs Old regime |
| Allow TDS modification to exceed calculated tax | Checkbox | Observed (state unknown) | Edge case for manual TDS override |

### IT Declaration State Machine

```
Locked → [Admin releases] → Open (employees can submit) → [Admin locks or window closes] → Locked
                                                         ↓
                                               Employees submit declarations
                                                         ↓
                                             Admin reviews (under Approvals)
```

### Key Info

Employees submit IT declarations through the **employee portal**, not the admin app. Once submitted, they appear in the admin's Approvals module for review.

Instructions: "Release IT Declaration or submit it on their behalf under Employees > Employee profile > Investments > IT Declaration."

---

## POI Settings

**URL:** `#/settings/preferences/proof-of-investment`

### Configuration Fields

| Field | Type | Current State | Notes |
|-------|------|---------------|-------|
| POI window status | Display | "POI is Locked" | |
| Release Proof Of Investments | Button | Active | Opens POI submission window |
| Process payroll with approved POI amount from | Month dropdown | March | TDS recalculation month |
| Allow employees to switch tax regimes | Checkbox | Observed | |
| Allow TDS modification during Payroll | Checkbox | Observed | |
| Mandate investment proof attachments | Checkbox | Observed | Forces file upload on POI submission |
| Mandate reviewer comments for partial amount approval | Checkbox | Observed | |

### Statutory Rule (POI)

"The approved POI amount will be considered for the payroll from **March** onwards to calculate and deduct income tax amount in subsequent payrolls."

This is the standard Indian tax computation cycle: POI submissions are typically finalized in Jan-Feb, and the approved amounts are applied from March onwards for the remaining months of the FY.

### Supporting Resources Linked

- POI Help Document (external Zoho help link)
- "Download and share this POI ebook" (downloadable PDF guide)
- "View Employees yet to submit POI" — top-right contextual link

---

## IT Declaration Approval Flow (Expected Based on Architecture)

Since no employees have submitted POI/declarations in the test org, we documented the configuration and empty states. Based on Zoho's architecture:

### What Admin Sees When Reviewing a POI Submission

1. Employee name, submitted declarations list
2. Declared amount per category (80C, HRA, medical, etc.)
3. Uploaded proof documents (attachments)
4. Options: Approve full amount / Approve partial amount / Reject with comment

### Approve vs Partial Approval

Zoho POI supports partial approval — admin can approve less than the declared amount. If "Mandate reviewer comments for partial investment amount approval" is enabled in Settings, a comment is required.

---

## Business Rules

1. **POI must be released before employees can submit** — the default state is "Locked"
2. **IT Declaration is separate from POI** — Declaration is the "planned" investment (at start of year); POI is the "proof" (at end of year)
3. **Admin can submit on behalf of employees** — for employees who can't access the portal
4. **Portal Status must be Enabled** for employees to self-submit
5. **Approved POI amounts affect TDS from March onwards** (configurable)
6. **"Yet To Confirm"** is a distinct state from "Approval Pending" — Zoho allows a two-stage review
7. **Tax Regime filter in POI** — New vs Old Regime; relevant because declared investments are only relevant for Old Regime. Our v1 is New Regime only.

## Critical Note for Our Build (New Regime v1)

Since v1 is New Regime only, the IT Declaration and POI approval flows have reduced importance:
- Under new regime, most deductions (80C, HRA, etc.) do NOT apply
- However, the Standard Deduction of ₹75,000 still applies automatically
- POI submission in new regime context: limited relevance — employees cannot claim most Section 80 deductions
- The "Allow employees to switch tax regimes" setting becomes relevant if we want to add old regime support later

## Cross-Module Impact

- Approved POI amounts feed into TDS calculation from March in the pay run
- Employee portal investment tab and admin investment tab are synchronized
- Payslips post-POI-approval show updated TDS amounts

## Screenshots

- `71-poi-approvals.png` — POI list (empty state + workflow diagram)
- `71-poi-view-dropdown.png` — View dropdown (All/Pending/YetToConfirm/Approved)
- `71-poi-unsubmitted-list.png` — Employees Yet to Submit POI list
- `71-poi-more-menu.png` — "Release POI" in more menu
- `71-poi-settings.png` — POI Settings page
- `71-it-declaration-settings.png` — IT Declaration Settings page

## Open Questions

- [ ] What do individual IT declaration line items look like (categories: 80C, 80D, HRA, etc.)?
- [ ] Can admin approve/reject individual line items within a declaration, or is it all-or-nothing?
- [ ] Is there an audit trail of approval decisions (who approved, when, what amount)?
- [ ] Under New Regime only v1, will this module be hidden or shown empty? Our build should clarify.
- [ ] How does "Yet To Confirm" differ from "Approval Pending" in the state machine?
- [ ] What notification is sent to employees when their POI is approved/rejected/partially approved?
