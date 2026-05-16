# DS-01: Design System — Overview and Navigation Patterns

**Module:** Design System / UI Patterns
**Observed from:** All pages visited during audit sessions
**Tested:** 2026-05-16

## Overview

Zoho Payroll uses a proprietary design system consistent across all pages. The design system is characterized by:
- Clean, card-based layout
- Left sidebar navigation (persistent, collapsible)
- Top band/header bar
- Content area with tabs for sub-navigation
- Dialog/drawer panels for entity detail views (not separate pages)
- Table-first data presentation

---

## Application Shell Structure

```
┌────────────────────────────────────────────────────────┐
│                     Top Band (Header)                   │
├─────────┬──────────────────────────────────────────────┤
│         │                                              │
│  Left   │         Main Content Area                   │
│ Sidebar │                                              │
│  Nav    │   [Breadcrumb / Page Title]                 │
│         │   [Action Buttons]                          │
│         │                                              │
│         │   [Tab Navigation (optional)]               │
│         │   [Content: Tables / Cards / Forms]         │
│         │                                              │
│         │   [Detail Drawer/Panel (slides in right)]  │
│         │                                              │
└─────────┴──────────────────────────────────────────────┘
```

---

## Top Band (Header)

### Components
| Component | Description |
|-----------|-------------|
| Zoho Payroll Logo | Left-aligned; clicking navigates to Dashboard |
| "Payroll" Label | Product identifier next to logo |
| Global Search | "Search in Employee" textbox with Advanced Search button |
| Trial Banner | "Your trial expires in N day(s)" + "Upgrade" button |
| Organization Switcher | "lerno" dropdown — switches between organizations |
| Refer and Earn | Referral program link |
| Notifications | Bell icon with badge count |
| Settings | Gear icon → opens Settings overlay |
| User Avatar | "A" circle → account menu |
| Cliq/Chat | Bottom-right float — Zoho Cliq integration |

### Organization Switcher
- Shows current org name ("lerno")
- Multi-org support — user can manage multiple payroll orgs from same Zoho account
- Switching org = switching tenant context

---

## Left Sidebar Navigation

### Structure
Persistent left sidebar, collapsible via "Collapse/Expand" button at bottom.

### Main Navigation Items (in order)
| Item | URL | Notes |
|------|-----|-------|
| Dashboard | `#/home/dashboard` | Onboarding checklist + help resources |
| Employees | `#/people/employees` | Employee master |
| Pay Runs | `#/payruns` | Split: Run Payroll + Payroll History |
| Approvals | (expandable) | Sub-items: Reimbursements, Salary Revision, POI |
| Taxes & Forms | (expandable) | Sub-items: TDS Liabilities, Form 24Q, Form 16, EPF/ESI/PT/LWF |
| Loans | `#/loans` | Loan management |
| Giving | `#/donations` | Charity campaign management |
| (unnamed listitem) | — | Unknown — not labeled in snapshot |
| Documents | `#/documents/folder` | Document management |
| Reports | `#/reports` | 39 pre-built reports |
| Settings | `#/settings` | Opens settings overlay |
| Contact Support | `#/support` | Support tickets |

### Collapsible Groups (Approvals, Taxes & Forms)
- Expandable/collapsible via clicking the group header
- Sub-items appear in the sidebar when expanded
- Arrow/chevron indicates expand state

---

## Settings Overlay

Settings opens as a full-height overlay panel (NOT a new page — URL changes but main content dims):
- Left navigation: Grouped sections (Organisation Settings, Module Settings, Extensions)
- Right content: Settings form for selected item
- Header: "All Settings" breadcrumb + org name + search + "Close Settings" button
- Settings search: Allows finding specific settings via keyword

---

## Page Patterns

### List Page Pattern
Used in: Employees, Loans, Payroll History, Approvals
```
[Page Title] [Action Buttons: New/Add/Filter/Export]
[Filter Bar (optional)]
[Data Table]
  - Sortable columns
  - Checkbox for bulk selection
  - Row actions (overflow/dropdown per row)
[Pagination or infinite scroll]
```

### Master-Detail Pattern
Used in: Employees (list + detail panel), Loans (list + detail panel)
```
[Left: Entity List with summary cards]    [Right: Selected entity detail]
  - Clickable rows                          - Header with entity ID and status
  - Entity avatars/icons                    - Action buttons
  - Key fields visible in list              - Tabbed sections
                                            - Related entity sections
```

### Tabbed Sub-page Pattern
Used in: Employee Profile, Pay Run Summary, Settings
```
[Page Header with breadcrumb]
[Tab Navigation: Tab1 | Tab2 | Tab3]
[Content for selected tab]
```
URL pattern: `#/base-url?selectedTab=tabname`

### Form Pattern
Used in: Create Employee, Create Loan, Add Salary Structure
```
[Page/Modal Title]
[Section headers grouping related fields]
[Field groups: Label + Input side by side]
[Required field indicator: *]
[Save] [Cancel] buttons at bottom
```

### Modal/Dialog Pattern
Used in: Off-Cycle Pay Run initiation, Payslip panel, Confirmation dialogs
```
[Backdrop (page dims)]
[Centered or side-panel dialog]
  [Close button (X)]
  [Dialog title]
  [Content: form or detail view]
  [Primary Action] [Cancel/Close]
```

---

## Status Badge Patterns

Observed across all modules:

| Color | Statuses Used |
|-------|--------------|
| Green | Active, Paid, Open (Loan), Approved |
| Blue | Draft, In Progress |
| Orange | Pending, Locked |
| Red | Inactive, Rejected, Overdue |
| Grey | Skipped, Inactive, Closed |

---

## Empty State Pattern

Consistent across all pages with no data:
```
[Illustration/Icon]
[Heading: "No [entity] found" or descriptive message]
[Sub-text: Contextual guidance]
[Primary CTA button: "Add [entity]" or "Create New"]
```

Examples observed:
- Loans: "The employee is yet to pay the first instalment through Zoho Payroll."
- Giving: "There are no active campaigns. Create campaigns to allow your employees to contribute for a cause."
- Web Tabs: "You haven't created any web tab yet."
- Reports: No data state per category
- Approvals: Filter-based empty state (no claims for selected period)

---

## Action Button Hierarchy

Consistent button hierarchy across all pages:

| Level | Style | Examples |
|-------|-------|---------|
| Primary | Filled (blue/orange) | Save, Create, Approve, Run Payroll |
| Secondary | Outlined | Edit, Cancel |
| Tertiary | Text-only or icon-only | View, Download |
| Danger | Red/outlined | Delete, Reject |
| Overflow | "Show dropdown menu" icon | Additional row/header actions |

---

## Notification/Toast Patterns

Expected (not directly captured):
- Success toast: Green banner, auto-dismisses after 3-5 seconds
- Error toast: Red banner, may persist until dismissed
- Warning banner: Orange, inline on page (e.g., "IT Declaration is Locked")
- Informational banner: Blue, inline (e.g., pending revision notice)

---

## Keyboard and Accessibility

- ARIA roles observed: `navigation`, `main`, `banner`, `complementary`, `region`, `dialog`, `table`, `rowgroup`, `columnheader`
- ARIA labels on most interactive elements
- `status` live regions for screen reader page load announcements
- Status announcements observed: "Pay Runs | Summary page loaded", "Settings Earnings page loaded"
- Keyboard navigation: Tab/Enter likely supported (not explicitly tested)

---

## Instant Helper

Present on most module pages as a floating button (? or help icon).
- Purpose: Context-sensitive help panel
- Content: Help articles and walkthroughs relevant to current page
- Appears to link to Zoho Payroll help documentation

---

## Gaps / Observations
- Design system is proprietary (Zoho's own) — not a standard library (not MUI, Tailwind, Ant Design)
- Color palette not formally documented (observed: blue primary, orange accents, grey neutrals)
- No dark mode observed
- Responsive behavior not tested (mobile web)
- The unnamed listitem in sidebar navigation — identity unknown

## Open Questions
- [ ] What is the unnamed listitem in the sidebar between Giving and Documents?
- [ ] Does the system support any keyboard shortcut navigation?
- [ ] Are there any print-specific styles (for payslips, challan documents)?
