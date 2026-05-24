---
name: project-payroll-saas
description: Indian payroll SaaS audit context — stack, URLs, auth, roles, and session continuity information
metadata:
  type: project
---

## Application Under Audit

**Product Name:** Indian Payroll SaaS (internal name)
**Reference Product:** Zoho Payroll
**V1 Scope:** New tax regime only (old regime deferred)
**Model:** Multi-tenant B2B, schema-per-tenant (PostgreSQL schema isolation)

## URLs
- Frontend: http://localhost:5173 (Vite dev server, running as background process)
- API: http://localhost:5000 (Docker container, port 8080 mapped to 5000)
- MailHog: http://localhost:8025 (email testing)
- MinIO: http://localhost:9000 / 9001
- Grafana: http://localhost:3001

## Auth System
- Token endpoint: POST /connect/token (OpenIddict, OAuth 2.0 password grant)
- client_id: payroll-api
- client_secret: dev-client-secret-2024 (env: VITE_CLIENT_SECRET)
- scopes: openid profile email offline_access payroll.api
- JWT claims: sub, email, role (string or string[]), tenant_id, tenant_slug, exp
- Auth state persisted in localStorage key: payroll-auth (Zustand persist middleware)
- 401 response auto-triggers logout

## Docker Services Running
- indian-payroll-api-1: port 5000 (status: unhealthy — note this)
- indian-payroll-db-1: PostgreSQL port 5432 (healthy)
- indian-payroll-redis-1: port 6379 (healthy)
- indian-payroll-minio-1: ports 9000-9001 (healthy)
- indian-payroll-mailhog-1: ports 1025, 8025
- indian-payroll-grafana-1: port 3001
- indian-payroll-prometheus-1: port 9090
- indian-payroll-pgbouncer-1: internal only

## Roles (from Payroll.Domain.Constants.Roles)
1. SuperAdmin — platform admin, can provision/manage tenants, has separate PlatformLayout
2. OrgAdmin — org-level admin, can manage org structure, users
3. HRManager — can create/manage employees
4. PayrollManager — payroll run management (inferred)
5. FinanceViewer — read-only finance reports (inferred)
6. Employee — self-service (inferred)

## Session History
- Session 1 (2026-05-14): Full code audit, Sessions covered: Login, ForgotPassword, SetPassword, Platform/Orgs, Platform/ProvisionOrg, Platform/OrgDetail, Employees, Branches, Departments, Designations, CostCentres
