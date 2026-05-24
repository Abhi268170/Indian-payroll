# Reports > Custom Report Builder

## Summary
Zoho Payroll does NOT have a freeform custom report builder. There is no drag-and-drop report designer, no SQL query interface, and no field picker for building reports from scratch.

## What Exists Instead

### 1. Customize Report Columns
Several pre-built reports have a "Customize Report Columns" feature accessible from within the report view. It shows a numeric badge indicating the count of available columns (e.g., "12" for EPF ECR Report, "7" for ESI Summary, "6" for ESI Monthly Summary, "5" for PT Summary, "5" for Employee-wise PT, "5" for LWF Summary, "4" for Annual PT).

**Behavior:** Users can show/hide specific columns within the report. The full column selection UI was not captured in this session due to navigation constraints, but the badge confirms this is available.

**Known reports with "Customize Report Columns":**
| Report | Column Count Badge |
|--------|--------------------|
| EPF ECR Report | 12 |
| ESI Summary | 7 |
| ESI Monthly Summary | 6 |
| Professional Tax Summary | 5 |
| Employee-wise PT Report | 5 |
| LWF Summary | 5 |
| Annual PT Report | 4 |

Notably absent from the customize-columns feature: Payroll Summary (which is a fixed two-column format), Form 24Q (pre-defined government form format).

### 2. More Filters (Advanced Filter Builder)
Every report has a "More Filters" button that opens an advanced criteria builder:
- Row structure: [Field selector] + [Operator dropdown] + [Value field]
- "Add Criteria" button to add more rows
- "Remove" button per row
- "Run Report" button to apply filters
- "Cancel" button to dismiss

This is a WHERE-clause style filter, not a report builder. It filters the existing report's dataset but does not add/remove columns or change the report structure.

### 3. Favorites
Reports can be starred/favorited. Favorited reports appear in the "Favorites" tab of the Reports Centre.

### 4. Shared Reports
Reports can be shared with other users in the org (via "Shared Reports" tab). The exact sharing mechanism was not captured but the feature exists.

### 5. Scheduled Reports
Reports can be scheduled for automatic email delivery. From within any report, a "Schedule" action is available. The scheduled reports appear in the "Scheduled Reports" tab.

**Empty state message:** "Currently, you don't have any scheduled reports. Go to a report and schedule it to view it here."

The scheduling UI (frequency, recipient email, format) was not fully captured in this session.

### 6. Zoho Analytics Integration
The sidebar displays a permanent upsell panel: "Advanced Financial Analytics for Zoho Payroll — Try Zoho Analytics" (links to `#/settings/integrations/zoho/analytics`).

This implies that for true custom reporting, pivot tables, cross-module analytics, and custom dashboards, users must subscribe to Zoho Analytics as an add-on. Zoho Payroll natively supports only the 39 pre-built reports.

## Comparison with Competitors

| Feature | Zoho Payroll | GreytHR | Keka |
|---------|-------------|---------|------|
| Custom report builder | No | Yes (basic) | Yes |
| Column customization | Yes (limited) | Yes | Yes |
| Advanced filters | Yes | Yes | Yes |
| Scheduled reports | Yes | Yes | Yes |
| Analytics integration | Zoho Analytics (paid add-on) | Tableau/PowerBI | PowerBI |

## Key Observations for Our Build

1. **Build a custom report builder from day one.** This is a clear gap in Zoho Payroll. Our target customers (HR/Finance teams) frequently need ad-hoc reports that pre-built reports don't address.

2. **Column customization should be universal** — every report should allow show/hide columns and column reordering. Zoho only offers this on select reports.

3. **Advanced filter builder (More Filters) is good UX.** The criteria row approach (field + operator + value) is intuitive. Our build should replicate this pattern.

4. **Report scheduling is table-stakes.** Implement scheduled reports with:
   - Frequency: Daily, Weekly, Monthly, Quarterly, Custom cron
   - Format: PDF, Excel, CSV (we add CSV)
   - Recipients: Email addresses (internal and external)
   - Time: Run at specific time in IST

5. **Favorites/bookmarking** is a nice UX feature to retain.

6. **Zoho Analytics dependency for custom reports is a business risk for customers.** Our build should provide a native report builder so customers don't need to pay for a separate BI tool for basic customization.
