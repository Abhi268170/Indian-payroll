# Settings > Automations

## Overview
The Automations section has 4 sub-pages within Settings, mapped to nav links under the "Automations" accordion. All pages are currently empty (no rules/actions/schedules configured for "lerno").

## Sub-pages

| # | Nav Label | URL | Page Title |
|---|-----------|-----|------------|
| 20 | Workflow Rules | `#/settings/automation/workflows` | Workflow Rules |
| 21 | Actions | `#/settings/automation/actions/alerts` | Actions (Alerts) |
| 22 | Schedules | `#/settings/automation/schedules` | Schedules |
| 23 | Workflow Logs | `#/settings/automation/logs/alerts` | Workflow Logs |

---

## Page 20: Workflow Rules (`#/settings/automation/workflows`)

### Purpose
Create conditional automation rules that trigger actions (email alerts, webhooks, custom functions, field updates) when specific payroll events occur.

### Usage Stats (Per Day Limits)
| Action Type | Used / Limit |
|------------|-------------|
| Custom Functions | 0 / 1,000 |
| Webhooks | 0 / 1,000 |
| Email Alerts | 0 / 500 |

### Empty State
> "You haven't created any Workflow Rules yet. With Workflow Rules, you can automate actions based on the specific conditions and criteria that you set..."

### Actions
| Button | Action |
|--------|--------|
| Add New | Opens "New Workflow Rule" modal |
| Add Workflow Rule | Same as Add New (in empty state body) |
| Module: All (dropdown) | Filter rules list by module |

### New Workflow Rule Modal
**Fields:**

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Workflow Rule Name | Text | Yes* | Display name for the rule |
| Description | Text | No | Optional |
| Module | Dropdown | Yes* | Which payroll module triggers the rule |

**Module dropdown options:**
- Pay Runs
- Employee
- Loan
- Reimbursement Claim

**Modal Buttons:** Next | Cancel

**Multi-step wizard implied** — "Next" button suggests further steps for: trigger event, conditions, and actions.

---

## Page 21: Actions (`#/settings/automation/actions/alerts`)

### Purpose
Pre-define reusable action components (Email Alerts, Webhooks, Custom Functions, Field Updates) that can be attached to Workflow Rules.

### Sub-tabs
| Tab | URL | Description |
|-----|-----|-------------|
| Alerts | `#/settings/automation/actions/alerts` | Email and in-app notification templates |
| Webhooks | `#/settings/automation/actions/webhooks` | HTTP webhook endpoints to call |
| Custom Functions | `#/settings/automation/actions/customfunctions` | Deluge (Zoho's scripting language) functions |
| Field Updates | `#/settings/automation/actions/fieldupdates` | Auto-update field values on trigger |

### Alerts Tab Content
**Header actions:** Configure Failure Preferences | Add Alert (button)

**Empty State:**
> "Alerts allow you to set up automated notifications and emails for important payroll events in Zoho Payroll. You can set up Email Alerts and In-App Notifications to alert users about any changes in your organisation."

**CTA buttons in empty state:** Email Alerts | In-app Notifications

**Module filter dropdown:** All (filters the alerts list by module)

---

## Page 22: Schedules (`#/settings/automation/schedules`)

### Purpose
Schedule periodic automation tasks using Deluge scripts, running at defined time intervals.

### Empty State
> "Automate repetitive payroll tasks with Schedule Tasks. Create predefined tasks using a simple deluge script and schedule them to run at specified time intervals. Save time and reduce the risk of errors."

### Actions
| Button | Action |
|--------|--------|
| Add New | Opens schedule creation form |
| Add Schedule | Same (in empty state body) |

---

## Page 23: Workflow Logs (`#/settings/automation/logs/alerts`)

### Purpose
Audit trail of all automation executions — what ran, when, and outcome. Filterable by type, module, status, and date range.

### Sub-tabs
| Tab | Logs Type |
|-----|-----------|
| Email Alerts | Alert execution history |
| Webhooks | Webhook call history |
| Custom Functions | Custom function execution history |
| Schedules | Scheduled task run history |
| Custom Button | Custom button trigger history |

### Filter Panel
| Filter | Type | Default | Options |
|--------|------|---------|---------|
| Status | Dropdown | All | (All, Success, Failure) |
| Module | Dropdown | All | (Pay Runs, Employee, Loan, Reimbursement Claim) |
| Date Range | Dropdown | Last Thirty Days | (various ranges) |

### Actions
| Button | Action |
|--------|--------|
| Apply Filter | Filters the log list |
| Export | Exports logs (format not confirmed — likely CSV) |

### Header action
"Configure Failure Preferences" — configures what happens when an automation fails (e.g., email the admin)

### Empty State
"There are no logs"

---

## Business Rules

1. **Per-day rate limits** — Custom Functions: 1,000/day; Webhooks: 1,000/day; Email Alerts: 500/day. Once exceeded, automations skip until the next day.
2. **Workflow Rules reference pre-defined Actions** — you first create Actions (alerts, webhooks, functions) separately, then reference them in Workflow Rules.
3. **Schedules use Deluge** — Zoho's proprietary scripting language. Not a standard scripting option; requires Zoho-specific knowledge.
4. **4 triggerable modules** — Pay Runs, Employee, Loan, Reimbursement Claim. No TDS/PT/PF-specific triggers visible.
5. **Failure preferences** — admins can configure what happens on automation failure (visible in both Actions and Workflow Logs pages).

## Cross-Module Impact
| Automation Trigger | Possible Action |
|-------------------|----------------|
| Payroll Run event | Email alert to employees/managers, webhook to external system |
| Employee created/updated | Field auto-update, notification |
| Loan approval | Email alert, webhook |
| Reimbursement claim submitted | Approval notification, webhook |

## Observations & Notes
1. **Deluge scripting = Zoho lock-in** — Custom Functions require Zoho's proprietary Deluge language, creating vendor dependency.
2. **No statutory automation triggers** — no built-in triggers for TDS filing deadlines, PF ECR generation, PT challan due dates. This is a gap vs. compliance calendar functionality.
3. **Rate limits** — 500 email alerts/day is extremely low for large orgs sending payslips (100 employees = 100 alerts per pay run). This may be per-org throttle, not per-automation.
4. **Workflow Logs as audit trail** — the 5 log sub-tabs (Alerts, Webhooks, Custom Functions, Schedules, Custom Button) provide good automation traceability.
5. For our build: Event-driven automation framework using domain events (Payroll.Domain events → Hangfire background jobs). Standard actions: Email, Webhook, Field update. No Deluge dependency — use C# Hangfire jobs instead. Rate limiting via Redis.

## Screenshots
`docs/ba-audit/settings/screenshots/20-automation-workflows.png`
