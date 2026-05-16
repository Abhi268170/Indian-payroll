# Approvals Module — Audit Index

**Audit Date:** 2026-05-15
**Auditor:** BA Agent (Playwright live audit)
**App:** Zoho Payroll India (payroll.zoho.in), Org: lerno

---

## Module Structure

The Approvals section is a sidebar nav item that expands to reveal three sub-modules:

```
Approvals (sidebar nav group)
├── Reimbursements          #/approvals/reimbursements
├── Proof Of Investments    #/approvals/proof-of-investment
└── Salary Revision         #/approvals/salary-revision
```

Loans are NOT under Approvals — they are a top-level sidebar item (`#/loans`).

Pay Run approval is NOT a separate Approvals screen — it is part of the Pay Run workflow itself.

---

## Approval Types Found

| # | Type | Route | Status | Notes |
|---|------|-------|--------|-------|
| 1 | Reimbursements | `#/approvals/reimbursements` | Operational (empty) | Requires reimbursement salary components to be configured per employee |
| 2 | Proof Of Investments (POI) | `#/approvals/proof-of-investment` | Operational (locked) | POI is locked; must be released from Settings or Approvals |
| 3 | Salary Revision | `#/approvals/salary-revision` | Operational (empty) | Only shows when custom approval workflow is configured |
| 4 | Pay Run | Settings > General > Pay Runs | Config only | Admin-initiated pay runs; approval type configurable via Settings |
| 5 | Loans | `#/loans` | Operational (empty) | Top-level module, NOT under Approvals nav |
| 6 | IT Declaration | Employee Portal | Not surfaced here | Managed via Settings > Claims and Declarations |

---

## Key Architectural Discovery

Zoho Payroll uses a **dual-track approval architecture**:

1. **Admin-initiated actions** (e.g., admin creates salary revision, admin creates loan) — auto-approved immediately, no approval queue needed.
2. **Employee-initiated actions** (e.g., employee submits POI, employee submits IT declaration via portal) — go through the Approvals queue for admin review.

The "Approvals" nav section is ONLY for employee-submitted items, not admin actions.

---

## Settings That Control Approvals

| Setting | Route | Controls |
|---------|-------|----------|
| Pay Run Approval | `#/settings/payrun/custom-approval/list` | Simple / Multi-level / Custom approval for pay runs |
| Salary Revision Approval | `#/settings/salary-revision/custom-approval/list` | Simple / Multi-level / Custom approval for salary revisions |
| Claims and Declarations > FBP | `#/settings/preferences/fbp` | Flexible Benefit Plan components |
| Claims and Declarations > Reimbursement | `#/settings/preferences/reimbursement` | Enable/configure reimbursement claims |
| Claims and Declarations > IT Declaration | `#/settings/preferences/it-declaration` | Release IT declaration window, TDS settings |
| Claims and Declarations > POI | `#/settings/preferences/proof-of-investment` | Release POI window, payroll processing month |

---

## Files in This Audit Set

- [00-approvals-index.md](00-approvals-index.md) — This file
- [68-approvals-list.md](68-approvals-list.md) — Overall approvals nav structure
- [69-payrun-approval.md](69-payrun-approval.md) — Pay run approval settings and architecture
- [70-salary-revision-approval.md](70-salary-revision-approval.md) — Salary revision form and approval flow
- [71-it-declaration-approval.md](71-it-declaration-approval.md) — IT Declaration and POI approval settings
- [72-loan-approval.md](72-loan-approval.md) — Loan creation form and approval architecture
- [73-reimbursement-approval.md](73-reimbursement-approval.md) — Reimbursement claims approval flow

---

## Screenshots Index

| Screenshot | Description |
|-----------|-------------|
| `00-approvals-nav.png` | Approvals sidebar nav expanded |
| `69-payrun-approval-settings.png` | Pay Run approval workflow selection screen |
| `70-salary-revision-approval-settings.png` | Salary Revision approval workflow settings |
| `70-salary-revision-list.png` | Salary Revision approvals list (empty, view dropdown open) |
| `70-salary-revision-form.png` | Salary Revision form (top half) |
| `70-salary-revision-payout-section.png` | Salary Revision form payout preferences section |
| `70-salary-revision-june-2026-ready.png` | Salary Revision form filled and ready to submit |
| `70-salary-revision-submit-result.png` | Validation error on missing effective date |
| `70-salary-revision-submitted-salary-details.png` | Post-submission salary details with pending revision notice |
| `70-salary-revision-detail-view.png` | Salary revision detail view with previous/new CTC comparison |
| `71-poi-approvals.png` | POI approvals list (empty state with workflow diagram) |
| `71-poi-view-dropdown.png` | POI view dropdown options |
| `71-poi-unsubmitted-list.png` | Employees yet to submit POI list |
| `71-poi-more-menu.png` | POI more menu showing "Release POI" option |
| `71-poi-settings.png` | POI Settings page |
| `71-it-declaration-settings.png` | IT Declaration settings page |
| `72-loans-empty.png` | Loans list (empty state) |
| `72-loans-view-dropdown.png` | Loans status view dropdown |
| `72-loans-create-form.png` | Create Loan form |
| `72-loans-type-empty.png` | Loan Name dropdown showing no configured loan types |
| `72-loans-employee-page.png` | Employee Loans tab empty state |
| `73-reimbursements-page.png` | Reimbursements approvals list |
| `73-reimbursements-view-dropdown.png` | Reimbursements view dropdown options |
| `73-reimbursements-more-menu.png` | Reimbursements more menu (Import/Export) |
| `73-reimbursements-add-form.png` | New Claim form (before employee selection) |
| `73-reimbursements-employee-dropdown.png` | Employee dropdown in New Claim form |
| `73-reimbursements-arjun-add-bill.png` | Error: employee has no reimbursements opted |
| `73-claims-declarations-settings.png` | Claims and Declarations settings (FBP tab) |
| `73-reimbursement-claims-settings.png` | Reimbursement Claims settings tab |
