# Giving > Overview

## URL / Navigation Path

- **Sidebar label:** Giving
- **Sidebar href:** `#/donations`
- **Actual working URL:** `#/donations` (reached via Ember `transitionTo('donations')`)
- **Module:** Giving (standalone module, between Loans and Documents in nav)
- **Page title (loaded):** `Zoho Payroll` (no specific title on list page)

## Critical Routing Observation

Navigating directly to `#/donations` from `#/loans` via hard navigation consistently redirects to `#/loans`. This is caused by the Ember `beforeModel` or `redirect` hook on the `donations` route. The hook appears to trigger a Loans-related check (possibly checking if Loans is in an open modal state). The issue is reproducible and represents a routing bug.

**Workaround discovered:** Use Ember's internal router:
```js
window.Ember.Application.NAMESPACES[0].__container__
  .lookup('router:main').transitionTo('donations')
```

This correctly loads the Giving module without redirect.

## Purpose

The Giving module enables HR/admin to create **donation campaigns** that employees can opt into through the Employee Portal. Employee contributions are deducted from payroll and applied as income tax exemptions (80G category). The feature positions itself as a CSR (Corporate Social Responsibility) payroll benefit.

## First Load State (Empty)

When no campaigns have been created:

**Header area:**
- Dropdown: "Active Campaigns" (with caret icon)
- Button: "New" (creates a new campaign, alternate entry point to the New Campaign form)

**Main content area (empty state):**
- Illustration/icon
- Heading: "There are no active campaigns"
- Sub-text: "Create campaigns to allow your employees to contribute for a cause"
- Button: "New Campaign" (primary CTA)

## Campaign List Views (Dropdown)

Clicking "Active Campaigns" reveals three filter options:

| Option | Description |
|--------|-------------|
| All Campaigns | Shows all campaigns regardless of status |
| Active Campaigns | Default view; shows only currently running campaigns |
| Completed Campaigns | Shows campaigns past their end date |

## API Calls on Load

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/v1/donations?filter_by=Status.Active&page=1&per_page=50` | Fetch active campaigns |

**Sample API Response (empty org):**
```json
{
  "code": 0,
  "message": "success",
  "donations": [],
  "page_context": {
    "page": 1,
    "per_page": 50,
    "has_more_page": false,
    "report_name": "Donation Summary",
    "applied_filter": "Status.Active"
  }
}
```

## Navigation Paths

**Entry points:**
- Left sidebar: "Giving" link
- Direct URL: `#/donations`

**Exit points:**
- Any sidebar module link
- New Campaign → `#/donations/new` (note: this sub-route redirects to `#/loans/new`, a confirmed routing bug)

## Ember Route Map for Giving

Routes discovered via Ember router inspection:

| Route Name | Path | Purpose |
|-----------|------|---------|
| `donations` | `#/donations` | List all campaigns |
| `donations.list` | `#/donations` | Sub-route for list rendering |
| `donations.new` | `#/donations/new` | Create new campaign (BROKEN — redirects to loans/new) |
| `donations.edit` | `#/donations/:id/edit` | Edit existing campaign |
| `donations.details` | `#/donations/:id` | Campaign detail view |
| `donations.details.pledged-employees` | `#/donations/:id/pledged-employees` | Employees who pledged |
| `donations.details.contributed-employees` | `#/donations/:id/contributed-employees` | Employees who contributed |

## Report Routes (Giving-related)

| Route | Description |
|-------|-------------|
| `reports.employee-donation-summary` | Summary of donations per employee |
| `reports.employee-donation-details` | Detailed donation transaction list |

## Key Observations for Our Build

1. **Campaigns are monthly-bounded** — end date is Month/Year granularity (not a specific date), aligning with payroll cycles.
2. **Feature is NOT premium-locked** — available on trial plan with no upgrade prompt.
3. **The API returns `"report_name": "Donation Summary"`** suggesting the list is implemented as a report query, not a simple entity fetch.
4. **Routing bug between Loans and Giving** — the Ember app has a state management issue where Loans modal state can intercept Giving route navigation. Our implementation should ensure clean route isolation.
5. **No NGO directory** — admins define their own campaign recipients. No third-party charity API integration observed.
