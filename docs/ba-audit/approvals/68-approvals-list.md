# Approvals > List Overview (Item 68)

## URL / Navigation Path

- Root URL: `https://payroll.zoho.in/#/approvals`
- Navigating to `#/approvals` renders an empty main panel (no default sub-route rendered)
- Sub-routes are accessed via the sidebar sub-links

**Sidebar Navigation Path:** Main Nav > Approvals (expandable) > [sub-item]

## Purpose

Central hub for admin to review and act on employee-submitted approval requests. Covers reimbursements, investment proofs, and salary revisions requiring approval before processing.

## Approval Types in Sidebar

| Nav Item | Route | Filter Applied by Default |
|----------|-------|--------------------------|
| Reimbursements | `#/approvals/reimbursements` | `?filter_by=Status.Submitted` (when accessed via sidebar link after first visit) |
| Proof Of Investments | `#/approvals/proof-of-investment` | None (shows current FY) |
| Salary Revision | `#/approvals/salary-revision` | None (shows current month) |

## Nav Badge / Count

- **No numeric badge** on the Approvals nav item itself — no unread count shown in the sidebar
- Within POI, a contextual warning badge appears: "2 employee(s) yet to submit POI" with a "View" action

## Approvals NOT in This Nav Section

The following approval-like flows exist elsewhere:

| Type | Location | Notes |
|------|----------|-------|
| Pay Run Approval | `#/payruns` | Inline in Pay Run list (Submit → Approve flow) |
| Loan Approval | `#/loans` (top-level nav) | No separate approval step; admin-initiated = auto-approved |
| IT Declaration | Employee Portal + `#/settings/preferences/it-declaration` | Admin reviews from employee's Investment tab |

## Common Page Layout (All Three Sub-Modules)

```
[Header]
  [Title dropdown: "All X / Pending X / Approved X / Rejected X"]    [CTA buttons] [...] [Filter] [Help]
[Filter Band]
  [Filter 1]  [Filter 2]  [Clear Filter]
[Content Area]
  [Table] or [Empty State]
```

## Common Filter Pattern

All three sub-modules share a consistent filter pattern:

| Filter | Type | Notes |
|--------|------|-------|
| Time Period | Month/Year date picker | Defaults to current month/year |
| Employees | Autocomplete dropdown | "Select an Employee" placeholder |
| Clear Filter | Button | Resets all filters |

## Common View Toggle Dropdown

All three sub-modules have a title dropdown with status-based views:

| Module | Views Available |
|--------|----------------|
| Reimbursements | All Claims / Pending Claims / Approved Claims / Rejected Claims |
| POI | All Investments / Approval Pending Investments / Yet To Confirm Investments / Approved Investments |
| Salary Revision | All Revisions / Pending Revisions / Approved Revisions / Rejected Revisions |

## Common Toolbar Buttons (All Sub-Modules)

| Button | Type | Action |
|--------|------|--------|
| View toggle dropdown | Dropdown | Changes status filter |
| "..." (more) | Dropdown | Module-specific overflow actions |
| Filter icon | Toggle | Shows/hides filter band (active state = highlighted) |
| "?" (Instant Helper) | Popup | In-app contextual help |

## Empty State

- **Message:** "No results found"
- **Sub-text:** "Looks like you don't have any results for the filter applied"
- **Illustration:** Zoho Genie (blue animated mascot) with a surprised expression

## Key Observations for Our Build

1. **No global approvals queue** — each approval type has its own dedicated page; there is no unified "pending approvals" dashboard
2. **Status-based views as the primary navigation** — the view toggle dropdown replaces tabs for status filtering
3. **Default filter is current month** — not "pending" — admin must switch to "Pending Claims" view to see actionable items
4. **No nav badge count** — this is a UX gap: admins won't know there are pending items without navigating to each section
5. **Three approval types only** in the Approvals section; Loans and Pay Run approvals live elsewhere

## Open Questions

- [ ] Does the Approvals nav item show a badge count when there are pending items in other orgs or with more data?
- [ ] Is there an email notification sent to admins when an employee submits something for approval?
- [ ] What triggers items to appear in Salary Revision approvals? (Our test showed admin-initiated revisions don't appear here — only employee-initiated via portal?)
