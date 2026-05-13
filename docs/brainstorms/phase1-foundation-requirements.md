---
date: 2026-05-13
topic: indian-payroll-phase1-foundation
master: docs/brainstorms/indian-payroll-saas-requirements.md
---

# Phase 1 — Foundation

## Goal

Deployable, containerised system with working auth, multi-tenancy, employee management, salary structure, basic payroll run (fixed components, no statutory deductions), and payslip. Everything built to enterprise-grade standards from day one — security, observability, audit trail all in place. Later phases add statutory compliance on top of this foundation without touching core infrastructure.

---

## Actors

- A1. Platform Super Admin — provisions/suspends tenants
- A2. Org Admin — company-level config, RBAC management
- A3. HR Manager — employee master, org structure, bulk imports
- A4. Payroll/Finance Manager — salary structures, payroll runs
- A5. Employee — self-service payslip access
- A6. Background Worker — async payroll batch

---

## Key Flows (Phase 1 scope)

- **F2 (partial):** Employee onboarding — create employee, assign CTC, generate personal salary structure instance, set PF/ESI/PT eligibility flags (stored, not yet computed). Employee becomes active for payroll.
- **F1 (partial):** Basic payroll run — fixed components only, gross computation, LOP placeholder (accepted but simple deduction, no statutory). Draft → Finalised state machine. Payslip generated.

---

## Infrastructure & Setup

- **I1.** Docker Compose with all containers from day one: `nginx`, `api`, `worker`, `db` (PostgreSQL 16), `pgbouncer`, `redis`, `minio`, `prometheus`, `grafana` (with Loki).
- **I2.** Solution structure: `src/Payroll.Api`, `src/Payroll.Application`, `src/Payroll.Domain`, `src/Payroll.Infrastructure`, `src/Payroll.Engine` (standalone class library), `web/` (React + Vite + TypeScript).
- **I3.** CI: GitHub Actions — build, test, lint on push; SSH deploy to VPS on merge to main.
- **I4.** Nginx: SSL termination, HSTS, CSP, X-Frame-Options, CORS locked to known origins.
- **I5.** Secrets via Docker secrets + env injection. Zero secrets in code or committed config.

---

## Requirements

**Configuration & Customisation**
- R0. Config-driven tenant customisation infrastructure in place (component formulas, grade tables, slab tables all stored as data).
- R0a. Statutory module toggle infrastructure — toggle table exists per tenant; all statutory calculators check toggle before applying. Toggles default to disabled until Phase 2 enables them.

**Multi-tenancy & Platform**
- R1. Schema-per-tenant isolation. `TenantDbContextFactory` resolves correct schema from scoped `TenantContext` on every request.
- R2. Hangfire job queue per-tenant isolation. Per-tenant concurrency cap in place.
- R3. Super admin: provision tenant (creates schema, runs migrations, seeds defaults), suspend tenant, view tenant list with active employee count.
- R4. Tenant schema migrations independently runnable. Migration runner iterates all schemas on deploy.

**Auth & Security**
- R48. RBAC: Super Admin, Org Admin, HR Manager, Payroll Manager, Finance Viewer, Employee. Org Admin inherits all tenant-level permissions. Single user can hold multiple roles.
- R49. PAN, Aadhaar, bank account number encrypted at rest (AES-256, application-layer, ASP.NET Data Protection). Column-level — not just TDE.
- R50. TLS 1.2+ enforced at Nginx. HTTP → HTTPS redirect.
- R51. Immutable audit trail table. PostgreSQL trigger prevents UPDATE/DELETE. Every payroll mutation, salary structure change, and statutory field change recorded: actor, timestamp, field, old value, new value.
- R52. Aadhaar masked in all API responses and UI (last 4 digits). Full reveal requires authorised role + writes audit log entry.
- Tenant resolution: subdomain + JWT double-lock. Mismatched token = 403.
- Rate limiting on all auth endpoints.
- PAN/Aadhaar masked before Serilog sinks — never logged in plaintext.

**Organisation & Employee Management**
- R5. Org structure: Company → Branches → Departments → Designations → Cost Centres. Each level optional. Employee must be assigned at minimum Department + Designation.
- R6. Employee master: all fields (personal, employment, statutory IDs, bank details, CTC). PAN mandatory. Aadhaar optional (warning if missing). Each field labelled mandatory/optional/conditional in UI.
- R58. Bulk employee import template (versioned CSV/Excel). Field descriptions in header rows.
- R59. Bulk import: per-row validation, summary report (N valid, M errors), partial import allowed, atomic per row.

**Salary Structure**
- R8. Tenant master salary components: earning heads (Basic, HRA, LTA, Special Allowance + custom), deduction heads (custom). Each component: formula rule (fixed, % of Basic, % of CTC, % of Gross), PF-eligible flag, TDS-taxable flag. Statutory components (PF, ESI, PT, LWF) scaffolded but disabled.
- R8a. Employee-level salary structure instance — personal copy of tenant master at onboarding. Per-employee component overrides. Isolated; changes to one employee never affect others.
- R8b. Push new component from master to existing employees (with default value). Audit-logged.
- R9. Pay grades with CTC ranges. Auto-suggest salary breakdown on employee assignment. Grade optional.

**Core Payroll Engine (Phase 1 — fixed components only)**
- R11 (partial). Payroll run computes: gross = sum of fixed earning components − LOP deduction. No statutory deductions in Phase 1 — TDS/PF/ESI/PT/LWF scaffolded as zero-value line items. Net pay = gross (statutory deductions all zero).
- R12. LOP deduction = (monthly gross / working days) × LOP days. Working days = calendar days minus Sundays by default; tenant can configure fixed value.
- R14. Payroll run idempotent — same inputs produce identical output.
- R15. Payroll run states: Draft → Finalised. Unlock Finalised requires A2/A4 with audit-logged reason.
- R16. Individual employee recompute within Draft run.
- R17. Batch async via Hangfire worker. In-app + email notification on completion/failure. Redis distributed lock prevents duplicate runs per tenant.
- R53. API responsive during batch. Batch runs in worker container, not API process.
- R55. Per-tenant job concurrency cap.

**Payslips & Self-Service**
- R44. Payslip PDF per employee per finalised run. Shows all earning components, deduction components (statutory as ₹0 in Phase 1), net pay.
- R45. Employee self-service portal: login, view own payslips, download PDF. PDFs optionally password-protected with PAN.

**Observability**
- OpenTelemetry traces → Prometheus. Grafana dashboards: API latency, payroll batch duration, queue depth, error rate per tenant.
- Serilog → Grafana Loki. Structured logs, queryable.
- Hangfire dashboard (internal, auth-gated).

---

## Out of Scope (Phase 1)

- All statutory deductions: TDS, PF, ESI, PT, LWF (Phase 2)
- Variable inputs file upload for ad-hoc components (Phase 2)
- Salary revision + arrears (Phase 3)
- FnF settlement (Phase 3)
- Off-cycle runs (Phase 3)
- Statutory returns: ECR, ESI challan, Form 24Q, Form 16 (Phase 2/3)
- Loan management (Phase 4)
- Bulk exports async (Phase 4)
- Mid-year YTD onboarding (Phase 3)
- Super admin metrics beyond basic tenant list (Phase 4)

---

## Success Criteria

- Docker Compose `up` starts all services cleanly on a fresh VPS.
- Super admin can provision a new tenant via API. Tenant gets isolated schema with all tables migrated.
- Org Admin can log in at `acme.yourpayroll.com`, create org structure, add employees (single + bulk), configure salary components, and run payroll for 100 employees.
- Payslip PDF generated and downloadable by employee from self-service.
- Token from `acme.yourpayroll.com` rejected with 403 on `infosys.yourpayroll.com`.
- Audit trail records all employee and payroll mutations; no row deletable.
- PAN and bank account stored as ciphertext in DB; decrypted only on authorised API response.

---

## Next Step

`/ce-plan` with this document as input.
