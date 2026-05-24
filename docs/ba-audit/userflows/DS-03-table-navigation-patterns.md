# DS-03: Design System — Tables, Navigation, and State Patterns

**Module:** Design System / UI Patterns
**Observed from:** All pages visited during audit sessions
**Tested:** 2026-05-16

---

## Table Component Patterns

### Column Types Observed

| Column Type | Example | Behavior |
|-------------|---------|----------|
| Text link | Employee Name, Loan Number | Click → navigates to detail or opens panel |
| Currency | Net Pay, Loan Amount | Indian format ₹X,XX,XXX.XX; right-aligned |
| Date | Payment Date, DOJ | Format: dd/MM/yyyy |
| Badge/Status | Paid, Open, Skipped, Active | Color-coded; not sortable |
| Action button | "View" (Payslip, TDS Sheet) | Triggers panel or navigation |
| Button | Employee Name in Pay Run | Opens detail or navigates |
| Overflow menu | "Show dropdown menu" | Reveals contextual actions per row |
| Checkbox | "Select this row" | Bulk selection |

### Sorting
- Sortable columns have an icon in the header (not consistently present — depends on column)
- Columns confirmed sortable in Loans table: Employee Name, Loan Number, Loan Amount
- Default sort order: Not documented (likely by creation date or employee ID)

### Pagination
- Not confirmed whether table uses pagination or virtual scroll
- Loans table showed 2 records — small enough not to paginate
- Employee list, Reports — likely paginated for large orgs

### Bulk Selection
- "Select all rows" checkbox in column header → selects all visible rows
- Individual "Select this row" checkbox per row
- Bulk actions appear in a toolbar above or below the table when rows are selected
- Skipped employees in pay run: NO checkbox (cannot bulk-select skipped rows)

---

## Navigation Patterns

### Breadcrumb Navigation
Not explicitly observed as a traditional breadcrumb trail. Instead:
- Settings uses a "All Settings > [Section]" path in the header
- Pay Run pages have "Back" link at top left with pay run type title

### Tab Navigation (Within Pages)
Used extensively for sub-section navigation within a single entity page:

**Pattern:**
```
[Tab 1] [Tab 2] [Tab 3]
       ↓ (selected)
[Content for selected tab]
```

URL reflects selected tab via query param: `?selectedTab=tabname`

**Examples:**
| Page | Tabs |
|------|------|
| Pay Run Summary | Employee Summary | Taxes & Deductions | Overall Insights |
| Employee Profile | (multiple tabs: Personal, Employment, etc.) |
| Loans | (Detail panel with sections) |
| Settings Salary Components | Earnings | Deductions | Benefits | Reimbursements |
| Employee Investments | IT Declaration | Proof of Investments |

### Expandable/Collapsible Sidebar Groups
- Approvals: click header → expands sub-items in sidebar
- Taxes & Forms: click header → expands sub-items
- Settings sections: click heading → expand/collapse sub-items

### Left Sidebar State
- Persistent: always visible
- Collapsible: "Collapse/Expand" button at bottom
- Active item highlighted (bold or colored)
- Groups expand/collapse on click

---

## Status/State Indicators

### Pay Run States
| State | Badge Color | Meaning |
|-------|-------------|---------|
| Paid | Green | Finalized and payment recorded |
| (implied) Draft | Blue/Grey | In progress, not yet finalized |
| (implied) Approved | Green | Approved, awaiting payment |

### Employee Pay Run States
| State | Display |
|-------|---------|
| Paid | "Paid on DD/MM/YYYY" in Payment Status column |
| Skipped | "Skipped" text with "Reason: Onboarding incomplete" in next cell |

### Loan States
| State | Meaning |
|-------|---------|
| Open | Active, EMIs being deducted |
| Paused | EMI deduction paused |
| Closed | Fully repaid or foreclosed |

### Employee States
| State | Display in Lists |
|-------|----------------|
| Active | Default |
| Inactive | Greyed out or filtered |

### Component States (Salary Components Settings)
| State | Meaning |
|-------|---------|
| Active | Can be used in salary structures |
| Inactive | Disabled — not assignable to new structures |

---

## Entity Detail Panel (Slide-in Drawer)

### When Used
- Pay Run Employee row click → Payslip detail panel
- Loan list row click → Loan detail panel
- Employee from pay run → Employee profile link

### Panel Structure
```
[Backdrop: main page visible but dimmed/pushed]
[Panel slides in from right]
  Header:
    [Close button (X) — top left or right]
    [Entity name (linked to full profile)]
    [Key metric: Net Pay / Loan Amount]
    [Identifiers: EMP ID, LOAN #]
  
  Content sections (scrollable):
    [Payment/status info]
    [Primary data table: Days, Earnings, Deductions]
    [Nested sections]
  
  Footer:
    [Primary action button(s): Download, Send, Record]
```

### Close Behavior
- X button closes the panel
- Clicking outside the panel (on the dimmed main content) may also close it
- ESC key behavior not confirmed

---

## Filter Patterns

### Common Filter Bar Pattern
Seen across: Approvals, Reports, Employees, Payroll History

```
[Filter | Label:] [Dropdown: Option 1 / Option 2 / All]
                  [Date Range / Month Picker]
                  [Employee Combobox]
[Apply / Close Filter button]
```

**Payroll History filter:** "Payroll Type:" dropdown (All / Regular Payroll / Off-Cycle)

**Approvals filter:** Claim Month + Payout Month + Employee

### Export Data Button
Present above tables on some pages.
- "Export Data" with icon
- Expected: triggers Excel/CSV download of visible data

---

## Link Patterns

### Internal Navigation Links
Format: `href="#/path"` (hash-based SPA routing)
Pattern: All internal links use hash routing (`#/`)

### External Links
Format: `href="https://..."` (full URL)
Examples: App Store links, Zoho Books, Help documentation
Expected behavior: Opens in new tab

### Entity Reference Links
Pattern: In a detail panel, the employee name links back to the full employee profile
Example: Payslip panel → "Arjun Mehta" → `#/people/employees/3848927000000032948`

---

## Overflow/Dropdown Menu Pattern

### "Show dropdown menu" Button
Universal pattern across all tables for row-level and header-level actions.
- Icon: Three dots vertical (⋮) or dropdown chevron (▼)
- ARIA: `button "Show dropdown menu"`
- On click: Opens a `listbox` positioned near the button
- Listbox items: `button` elements with action labels
- Dismissal: Click outside, or navigate away

### Actions per Context
| Context | Available Actions |
|---------|-----------------|
| Pay Run header (PAID) | Download all Payslips, Download all TDS Worksheets, Show Downloads, Delete Recorded Payment |
| Employee row in Pay Run | (not captured — dropdown dismissed before snapshot) |
| Loan row | Edit Loan, Pause Instalment Deduction, Delete Loan |
| Salary Component row | Edit, Duplicate/Clone, Deactivate, Delete |

---

## Progress Indicators

### Dashboard Onboarding Checklist
- "5/7 Completed" text progress
- Individual step: checkmark icon (✓) when complete
- Incomplete step: numbered circle
- Steps are linked to their configuration pages
- Steps marked complete when visited (not necessarily configured)

### Loan Progress Bar
- Visual bar showing repayment progress (0% for new loans)
- Percentage calculated from: Amount Repaid / Total Amount

### Pay Run Status Flow
(Inferred from UI states — full approval workflow requires active pay run)
```
[Draft] → [Submitted for Approval] → [Approved] → [Paid]
```

---

## Instant Helper (Contextual Help)

Present on: Employees, Pay Run, Loans, Reports, Settings

- Floating button: "?" icon or similar
- Position: Bottom-right or top-right of content area
- Click: Opens a contextual help slide-in panel
- Content: Links to relevant help articles, tutorials, setup guides

---

## Gaps / Observations
- Exact animation/transition styles not captured (CSS transitions for drawer slide-in)
- Focus management on modal open/close not tested
- Print stylesheet not investigated
- RTL (right-to-left) support: not tested; Indian context is LTR

## Open Questions
- [ ] Is the SPA routing hash-based (#/) or uses History API?
- [ ] Does the application cache filter state between sessions (e.g., last selected month)?
- [ ] Are tables virtualized for large datasets (100+ employees)?
- [ ] What is the maximum number of columns visible on a 1366x768 viewport without horizontal scroll?
