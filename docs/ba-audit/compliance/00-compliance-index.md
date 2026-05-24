# Compliance Module — Audit Index
**Session:** Phase 5–6 (Statutory Compliance + Form 16)
**Date:** 2026-05-15
**Org:** lerno (trial org, EPF/ESI not configured, PT/LWF auto-provisioned for Kerala)
**Auditor:** BA Agent

---

## Navigation Discovery

The compliance module in Zoho Payroll is **not a top-level nav item labelled "Compliance"**. It is accessed via:

1. **Taxes & Forms** accordion in the left sidebar — unlocked by clicking "View Details" in the persistent bottom banner ("Track TDS Liabilities and Generate Form 24Q").
2. **Settings > Setup & Configurations > Statutory Components** — for EPF/ESI/PT/LWF configuration.

Before enabling the feature, the sidebar showed only "Form 16" as a top-level link. After clicking Enable:
- Sidebar "Taxes & Forms" accordion expanded with 4 items: TDS Liabilities, Challans, Form 24Q, Form 16.
- The bottom banner disappears (feature considered enabled).

**Important:** The TDS/Challans/Form 24Q module only applies from **April 2026 onwards** (system enforced). Historical pay runs prior to enabling do not generate liabilities retroactively.

---

## File Index

| Item | File | Area | Status |
|------|------|------|--------|
| 74 | [74-epf-ecr-challan.md](74-epf-ecr-challan.md) | EPF — ECR + Challan | Documented (EPF not configured) |
| 75 | [75-epf-uan-management.md](75-epf-uan-management.md) | EPF — UAN per employee | Documented (conditional on EPF enable) |
| 76 | [76-esi-challan.md](76-esi-challan.md) | ESI — Challan + config | Documented (ESI not configured) |
| 77 | [77-pt-challan.md](77-pt-challan.md) | PT — Kerala slabs, challan | Documented (Kerala auto-configured) |
| 78 | [78-lwf-challan.md](78-lwf-challan.md) | LWF — Kerala monthly | Documented (Kerala disabled) |
| 79 | [79-tds-form24q.md](79-tds-form24q.md) | TDS Liabilities + Challans + Form 24Q | Fully documented |
| 80 | [80-form16-part-a.md](80-form16-part-a.md) | Form 16 — Part A | Documented (gate: Tax Deductor missing) |
| 81 | [81-form16-part-b.md](81-form16-part-b.md) | Form 16 — Part B | Documented (salary breakup source) |
| 82 | [82-form16-bulk-generate.md](82-form16-bulk-generate.md) | Form 16 — Bulk generate + email | Documented (4-step flow) |

## Key Cross-Cutting Findings

- EPF and ESI are not configured → employee-level statutory info shows only PT checkbox
- PT is per work-location, auto-seeded from org address state (Kerala)
- LWF for Kerala: INR 50 each side, Monthly, currently Disabled
- TDS module is feature-flag gated — must explicitly "Enable" via modal
- Form 24Q Q1 FY2026-27 already provisioned (Due 31/07/2026); TDS = INR 0 for test org
- Form 24Q generates an FVU-compatible text file (not a direct TRACES upload)
- Form 16 is blocked until Tax Deductor details are configured in Settings > Tax Details
- Reports: 39 system-generated reports, 8 in Statutory category (EPF Summary, EPF ECR, ESI Summary, ESI Monthly, PT Summary, Employee-wise PT, Annual PT, LWF Summary)
