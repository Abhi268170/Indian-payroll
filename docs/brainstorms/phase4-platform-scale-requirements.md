---
date: 2026-05-13
topic: indian-payroll-phase4-platform-scale
master: docs/brainstorms/indian-payroll-saas-requirements.md
---

# Phase 4 — Loans, Platform & Scale

## Goal

Complete the product: employee loan management with EMI deductions and FnF recovery; full bulk import/export infrastructure (async for large tenants); super admin dashboard with cross-tenant metrics; and performance hardening to verify the 10,000-employee 30-minute SLA under load.

**Prerequisite:** Phase 3 complete and deployed.

---

## Actors

- A1. Platform Super Admin — full cross-tenant metrics, tenant ops
- A2. Org Admin — approve loan waivers in FnF
- A3. HR Manager — bulk loan import
- A4. Payroll/Finance Manager — loan management, async export downloads
- A5. Employee — loan balance visibility in self-service
- A6. Background Worker — async exports, loan EMI batch deduction

---

## Key Flows (Phase 4 scope)

- **F5 (complete):** All bulk imports with consistent validation pattern — employees, salary structure assignment, variable inputs (already live), YTD data (already live), loan data.
- **F6 (complete):** All bulk exports with async delivery for >1,000 employees — pay register, payslip ZIP, TDS working sheet, PF ECR, ESI challan, PT register, Form 24Q data, Form 16 ZIP.

---

## Requirements

**Loan Management**
- R61. Loan record: principal, EMI/month, start month, optional end date. Multiple loans per employee.
- R62. Monthly payroll run auto-includes active loan EMIs as named deduction line items. Outstanding balance updated after each deduction.
- R63. Loan bulk import CSV: Employee ID, loan name, principal, EMI, start month. Validation: employee active, EMI ≤ principal, start month current or future.
- R64. FnF: outstanding loan balance auto-fetched across all active loans, shown as recovery deduction. A4 can override with audit-logged reason.
- R65. Loan dashboard per employee: principal, total recovered, outstanding balance, projected closure month, month-by-month recovery history.

**Bulk Imports (complete)**
- R58. Employee import template versioned with changelog. All fields + descriptions.
- R59. Per-row validation, summary report, partial import, atomic per row — applies to all bulk import types.
- F5 bulk import types: employees, salary structure assignment, variable inputs (already Phase 2), YTD data (already Phase 3), loan data.

**Bulk Exports (async)**
- R54. Exports for >1,000 employees run asynchronously via Hangfire job. A4 receives in-app + email notification with download link. MinIO stores the generated file.
- R60. All export types async-capable: pay register, payslip bulk ZIP, PF ECR, ESI challan, PT register, Form 24Q data, Form 16 bulk ZIP, TDS working sheet. All export events audit-logged.

**Platform — Super Admin**
- R3 (complete). Super admin dashboard: provision tenant, suspend tenant, per-tenant metrics (active employee count, last payroll run date, run status, error count).
- R4. Tenant schema backup tooling — per-tenant dump/restore without affecting other tenants.
- R2 (hardened). Per-tenant job concurrency cap verified under load. Worker container horizontal scaling tested.

**Performance Hardening**
- R17 (verified). Batch payroll for 10,000 employees completes within 30 minutes end-to-end under representative load (realistic salary structures, all statutory modules enabled, PDF generation included).
- R55 (verified). One tenant's 10k-employee batch does not delay another tenant's batch or API responsiveness.
- Load profiling: identify and fix top bottlenecks (DB query plans, PDF generation throughput, MinIO upload latency).

---

## Out of Scope (Phase 4)

- Loan interest computation (interest-free only in v1)
- PT/LWF for states beyond Kerala (future)
- Old tax regime (explicitly never in v1)
- Mobile app (future)
- Accounting integrations beyond CSV export (future)
- Perquisite taxation, Section 192(2B) (future)

---

## Success Criteria

- Employee with 2 active loans sees correct EMI deductions in monthly payslip; outstanding balance reduces correctly each month.
- FnF with outstanding loan balance shows correct recovery; waiver by A2 audit-logged with reason.
- Async export of pay register for 5,000 employees completes within 5 minutes and download link delivered via notification.
- 10,000-employee payroll batch completes within 30 minutes end-to-end with all statutory modules enabled.
- Simultaneous payroll runs for 3 tenants (each 3,000 employees) do not impact each other's API response times.
- Super admin can provision a new tenant, view its metrics, and suspend it without touching any other tenant's data.

---

## Next Step

`/ce-plan` with this document as input (after Phase 3 is deployed).
