# Employees > Employee List — Empty State

## URL / Navigation Path
- Route: `#/people/employees`
- Full URL: `https://payroll.zoho.in/#/people/employees`
- Entry points:
  - Sidebar link "Employees" (href `#/people/employees`)
  - Dashboard onboarding Step 6 "Add Employees" > "Complete Now" button
- Page title: "Employees | Zoho Payroll"

## Purpose
Landing page for the Employees module. When no employees have been added, shows an onboarding empty state prompting the user to add or import their first employee. Also surfaces a Zoho People integration shortcut.

## Layout
- **Top bar**: Global navigation (same as all pages) — logo, global search ("Search in Employee"), trial expiry notice, Upgrade button, org switcher, Refer & Earn, Notifications, Settings, Avatar.
- **Left sidebar**: Standard 10-item navigation (Getting Started, Dashboard, Employees [active], Pay Runs, Approvals, Form 16, Loans, Giving, Documents, Reports, Settings). Collapse/Expand toggle at bottom.
- **Main content area**: Two sub-sections:
  1. **Toolbar row** (top of main): Active Employees tab/button with dropdown chevron, then action buttons on right.
  2. **Empty state body**: Centred illustration + heading + description + two CTA buttons + Zoho People sync promo.
- **Maintenance banner** (bottom overlay): "Planned India Data Center Maintenance on 10th May 2026 and 17th May 2026, between 06.30AM to 09.30AM IST." with "Know more" link and "Don't show again" dismiss.

## Fields (table: Field | Type | Required | Options | Help Text | Validation)

No data input fields on this page in empty state. All interaction is via buttons and links.

## Buttons & Actions

| Button / Link | Type | Location | Target / Behaviour |
|---|---|---|---|
| Active Employees | Tab/button with dropdown chevron | Toolbar top-left | Filters list to active employees; dropdown likely reveals other status filters (Inactive, All) — not yet expanded |
| Add | Primary button (blue, with icon) | Toolbar top-right | Opens Add Employee wizard/flow |
| Show dropdown menu (chevron next to Add) | Secondary button | Toolbar top-right | Likely reveals: "Add Employee" + "Import Employees" options |
| (icon button, unlabelled) | Icon button | Toolbar top-right | Purpose unclear — possibly column customisation or export |
| Instant Helper | Icon button | Toolbar top-right | Opens guided walkthrough panel |
| Add Employee | Primary CTA button | Empty state body | Triggers employee creation flow |
| Import Employees | Link | Empty state body | Navigates to `#/people/employees/import?entity_type=employee_basic_personal_details` |
| Connect Now | Link | Zoho People promo strip | Navigates to `#/settings/integrations/zoho` |

## Empty State Content

- **Illustration**: Custom graphic (ref=e566)
- **Heading**: "Get your employees onboard"
- **Body text**: "Capture all necessary details about your employees and manage their salary, allowances and reimbursement details in this module."
- **Primary CTA**: "Add Employee" button
- **Secondary CTA**: "Import Employees" link (not a button)
- **Zoho People promo**: Icon + "Sync employees from Zoho People directly into Zoho Payroll." + "Connect Now" link to integrations settings

## Conditional Logic

- When employees exist: empty state body is replaced by a data table (not visible in this state).
- "Active Employees" dropdown presumably changes status filter — full options not yet visible.

## Cross-Module Impact

- "Import Employees" links to import flow with `entity_type=employee_basic_personal_details` — implies import is entity-type scoped (personal details only on this path; other entity types may exist for salary, bank, etc.).
- "Connect Now" links to Zoho People integration settings — cross-product dependency for HRMS sync.

## Key Observations for Our Build

1. **Status filter as tab, not dropdown**: Zoho uses a tab-style primary filter ("Active Employees") rather than a separate filter panel. Simple and prominent.
2. **Two add paths clearly separated**: Direct add vs bulk import are distinct CTAs — good UX pattern to replicate.
3. **Zoho People sync promo**: A cross-sell hook baked into the empty state. We will not have this but could offer CSV import or API import in the same position.
4. **Import URL includes `entity_type` query param**: This signals a multi-entity import system where different data types have different templates. We should consider the same separation (personal, salary, bank, statutory).
5. **No column headers or filter chips visible in empty state**: Columns and filter UI only appear once data exists — reduces cognitive load on fresh setup.
6. **Global search in header is labelled "Search in Employee"**: The search scope changes context-sensitively based on the active module — important UX detail.
7. **Trial expiry notice in header**: "Your trial expires in 14 day(s)" — persistent SaaS trial reminder. Not relevant to our build but worth noting for future commercial layer.

## Screenshots
- `screenshots/34-employee-list-empty.png` — Full page screenshot of empty state
