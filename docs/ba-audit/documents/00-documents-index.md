# Documents Module — Audit Index

**Audit Date:** 2026-05-15
**Module URL:** `#/documents/folder`
**API Base:** `GET /api/v1/folders`, `GET /api/v1/documents`

## Files in this Directory

| File | Coverage |
|------|----------|
| `95-documents-list.md` | Module layout, folder types, sidebar, filter system, storage limits |
| `96-system-generated-docs.md` | Auto-generated documents (payslips, Form 16), document API model |
| `97-upload-document.md` | Folder creation, upload mechanism, file format rules, employee visibility |
| `98-esignature-flow.md` | E-signature investigation — feature absent in current version |

## Module Summary

The Documents module is a file management system embedded in Zoho Payroll. It supports two folder types: Org folders (organization-wide, e.g., policy documents, offer letters) and Employee folders (per-employee, e.g., personal KYC docs). Documents are uploaded as ZIP archives containing PDFs named by employee ID. There is no e-signature feature. A document expiry reminder system exists in Settings.

## Key Findings

1. Two distinct folder types: `org_public_folder` and `payroll_employee_folder` with different data contexts.
2. Storage limit: 1GB per 100 employees.
3. Upload model: ZIP of PDFs named by employee ID (batch upload only — no single file per employee UI).
4. Document expiry reminders configurable in Settings > Employee > Document.
5. No e-signature feature present in any route, API, or Ember router.
6. Payslips and Form 16 are NOT stored in the Documents module — they have separate download mechanisms.
7. Module has three top-level routes: `documents.folder` (unified view), `documents.organization-folder` (org-only), `documents.employee-folder` (employee-only).
