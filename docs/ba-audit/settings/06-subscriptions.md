# Settings > Subscriptions

## URL
`#/settings/subscription-details`

## Purpose
Displays the current subscription plan, usage statistics, and the full feature entitlement list for the organisation. Provides access to plan upgrade flow.

## Page Layout
Three sections:
1. **Plan Details** — current plan name
2. **Usage Stats** — employee seat usage
3. **Features** — complete list of included features, grouped into Core Features (General + Custom Automation) and Premium Features

Header: "Subscription Details" heading + "View Feature Details" button + "Upgrade Plan" button

## Fields (Read-only Display)

| Field | Type | Value (Current) | Notes |
|-------|------|-----------------|-------|
| Plan Name | Display label | **14-days free trial** | Shown in uppercase/bold |
| Employee usage | Counter | 0 / - used | Format: `{used} / {limit} used`. Limit shows "-" on free trial (unlimited or unset) |
| Employee limit link | Button | "(View Details)" | Opens a breakdown of seat usage |

## Feature Entitlement List

### Core Features — General (24 features)
All marked as included (green check icon) in the current trial plan:

| # | Feature |
|---|---------|
| 1 | Reporting Tags |
| 2 | Document Module |
| 3 | Leave Management |
| 4 | Basic Attendance Management |
| 5 | Advanced Approval |
| 6 | Salary Templates |
| 7 | Letter Templates |
| 8 | Salary Hold |
| 9 | Custom Salary Components |
| 10 | Approvals |
| 11 | Reminder |
| 12 | Loans |
| 13 | Forms |
| 14 | Activity Logs |
| 15 | Audit Trail |
| 16 | Salary Components Custom Formula |
| 17 | Custom Alerts And Reminders |
| 18 | Contractors |
| 19 | New Joinee Arrears |
| 20 | Employee Statutory Bonus |
| 21 | Bonus Pay Run |
| 22 | Off Cycle Payroll |
| 23 | Donation |
| 24 | Custom View |
| 25 | Scheduled One Time Earnings |

### Core Features — Custom Automation (5 features)
| # | Feature |
|---|---------|
| 1 | Custom Fields |
| 2 | Custom Roles |
| 3 | Workflow Rules |
| 4 | Custom Functions |
| 5 | WebTabs |

### Premium Features (1 visible)
| # | Feature |
|---|---------|
| 1 | Custom Buttons and Links |

Note: Premium features are listed separately — likely locked/greyed out on non-premium plans (icon state not confirmed from snapshot; all may show as available on trial).

## Buttons & Actions

| Button | Label | State | Action |
|--------|-------|-------|--------|
| View Feature Details | "View Feature Details" | Always enabled | Opens detailed plan comparison page (likely external Zoho Payroll pricing page or modal) |
| Upgrade Plan | "Upgrade Plan" | Always enabled | Opens plan upgrade/purchase flow |
| View Details (usage) | "(View Details)" | Always enabled | Opens seat usage breakdown popup/panel |

## Tabs (if any)
None.

## Conditional Logic
1. **Feature availability indicators** — Each feature shows an icon (green check = included, grey/locked = not included). On free trial, all features appear enabled.
2. **Employee limit** — shows `-` as limit on free trial; on paid plans would show the purchased seat count.
3. **Premium Features section** — appears as a distinct category; features here may show locked state on lower-tier plans.

## Cross-Module Impact
The subscription plan gates access to specific modules and features across the entire application:

| Plan Gate | Affected Modules |
|-----------|-----------------|
| Leave Management | `#/settings/holiday-leave/enable-module` |
| Loans | `#/settings/loan/custom-field/list` |
| Contractors | `#/settings/employee/contractor` |
| Custom Fields | Custom field configuration across all modules |
| Custom Roles | `#/settings/users-roles/roles` |
| Workflow Rules | `#/settings/automation/workflows` |
| Custom Functions | `#/settings/automation/actions` |
| Reporting Tags | `#/settings/advanced-reportingtags` |
| Salary Templates | `#/settings/salary-templates` |
| Audit Trail | `#/settings/automation/logs` |
| Advanced Approval | Approval workflows in pay runs, salary revisions |
| Off Cycle Payroll | Supplementary payroll runs outside normal schedule |
| Bonus Pay Run | Dedicated bonus processing run |
| Direct Deposits | `#/settings/direct-deposit` |

## Observations & Notes

1. **Trial plan is fully-featured** — the 14-day trial enables all features including Premium ones, making it hard to observe locked/gated states from audit alone.
2. **Employee seat count shows "0 / -"** — confirms no employees have been added yet. The "-" limit is unusual; paid plans would show a specific seat count (e.g., "0 / 50 used").
3. **"View Feature Details"** likely opens Zoho Payroll's public pricing/feature comparison page — a useful reference for understanding tier boundaries.
4. **Feature list is exhaustive and reveals the full product scope** — this page is the most comprehensive view of what Zoho Payroll can do as a product.
5. **Key features relevant to our build**: Salary Components Custom Formula, New Joinee Arrears, Employee Statutory Bonus, Off Cycle Payroll, Bonus Pay Run — all are in Core (not Premium), confirming they should be part of our v1 core scope.
6. **Donation feature** — unusual for a payroll product; likely refers to PM CARES / charitable donation deduction from salary.
7. **Scheduled One Time Earnings** — one-time salary components scheduled for a future pay period; important for bonus and incentive management.
8. For our own SaaS: subscription plan feature gating should be implemented via a feature flag system tied to the tenant's subscription tier, not hardcoded per tenant.

## Screenshots
`docs/ba-audit/settings/screenshots/06-subscriptions.png`
