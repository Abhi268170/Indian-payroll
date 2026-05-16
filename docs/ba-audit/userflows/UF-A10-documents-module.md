# UF-A10: Documents Module

**Module:** Documents (left sidebar)
**Tested:** 2026-05-16
**Route:** `#/documents/folder`
**Page title:** "All Documents | Documents | Zoho Payroll"

---

## Findings

### 1. Module Overview

**Purpose:** Centralised document management for HR/payroll documents. Two-tier: Org-level documents and Employee-level documents. Supports file upload, folder organisation, and optionally exposes employee-specific documents in the employee portal.

**Entry point:** "Documents" in left navigation sidebar.

---

### 2. Documents Landing Page

**Route:** `#/documents/folder`
**Page title:** "All Documents | Documents | Zoho Payroll"

**Left sub-navigation (Documents sidebar):**
| Item | Route | Purpose |
|------|-------|---------|
| All Documents | `#/documents/folder` | View all documents across org |
| Offer Letters | `#/documents/folder?folder_id=3848927000000034270&folder_name=Offer%20Letters` | Pre-built folder for offer letters |
| Personal Documents | `#/documents/folder?folder_id=3848927000000034272&folder_name=Personal%20Documents` | Pre-built folder for personal docs |
| Trash | `#/documents/trash` | Deleted documents |

**Folder types:**
| Type | Description |
|------|-------------|
| Org Folder | Documents visible to admins only (or shared with employees selectively) |
| Employee Folder | Documents attached to a specific employee record |

**Storage limit:** "1GB / 100 employees" — storage quota is employee-count-based. 10MB per employee estimate.

---

### 3. Filter Options

| Filter | Type | Options |
|--------|------|---------|
| Employee Status | Dropdown | Active, Inactive, etc. |
| Employee | Dropdown | All employees |

Filtering lets admins see documents uploaded for a specific employee or status group.

---

### 4. File Upload

**Upload mechanism:** Drag & Drop zone OR browse button
**UI text:** "Drag & Drop File Here — or —" (with browse button implied)
**Accepted file types:** Not directly observed. Expected: PDF, JPEG, PNG, DOCX (standard HR doc formats)
**Max file size:** Not observed. Expected: limited by 1GB org storage quota.

---

### 5. Pre-Built Folders

**Offer Letters:**
- Route: `#/documents/folder?folder_id=3848927000000034270&folder_name=Offer%20Letters`
- Purpose: Store offer letter PDFs per employee
- Expected: Per-employee sub-folders or tagging

**Personal Documents:**
- Route: `#/documents/folder?folder_id=3848927000000034272&folder_name=Personal%20Documents`
- Purpose: PAN card scans, Aadhaar, certificates, etc.
- Expected: Per-employee access control

**Trash:**
- Route: `#/documents/trash`
- Soft delete — documents recoverable from trash before permanent deletion

---

### 6. Employee Portal Document Access

**Setting location:** Settings > Employee Portal (`#/settings/portal/preferences`)
**Toggle:** "Show documents in employee portal" — Enable/Disable
**Default state:** Disabled (checkbox unchecked)

When enabled:
- Employees can view documents uploaded to their employee folder via the portal
- Org-level documents are NOT visible to employees (admin-only)
- Per-document visibility controls may exist (not tested)

---

### 7. Employee-Level Documents (from Employee Profile)

Based on prior audit session (UF-17-upload-employee-documents.md):

**Route:** `#/people/employees/{id}/salary-details` or dedicated documents tab

Employees have documents uploaded directly to their profile:
- Identity documents (PAN, Aadhaar)
- Qualification certificates
- Employment contracts
- Onboarding documents

These feed into the Documents module's "Employee Folder" structure.

---

### 8. Document Module Capabilities Summary

| Capability | Status |
|-----------|--------|
| Folder creation (custom) | Expected — not tested |
| File upload (drag & drop) | Confirmed |
| Pre-built folders | Confirmed (Offer Letters, Personal Documents) |
| Trash/soft delete | Confirmed |
| Employee-filtered view | Confirmed |
| Portal visibility toggle | Confirmed (org-level setting) |
| E-sign integration | Not observed — may require Zoho Sign integration |
| Document templates | Not observed in this module — PDF Templates in Settings is separate |
| Bulk upload | Not tested |
| Document expiry/renewal tracking | Not observed |

---

### 9. Related Settings

**PDF Templates** (`#/settings/templates/regular-payslip`): This is for payslip PDF template customisation — separate from the Documents module.

**Document Management toggle in Portal settings:** Governs whether employees see their documents in the portal.

---

## Screenshots / Files

- `documents-module.png` — Documents module landing page with folder structure and upload zone

---

## Gaps / Open Questions

- [ ] **Custom folder creation:** Can admins create new folders beyond Offer Letters and Personal Documents?
- [ ] **Per-employee folder structure:** Is each employee's folder automatically created? Or is it a flat list with employee tags?
- [ ] **E-sign capability:** Does Zoho Payroll integrate with Zoho Sign for offer letter e-signing? Not observed.
- [ ] **Document templates:** Is there an offer letter template generator within Documents, or is it upload-only?
- [ ] **Per-document visibility:** Can individual documents be marked "visible to employee" or "admin only" regardless of org-level portal toggle?
- [ ] **File type restrictions:** What file types are accepted? Is PAN Aadhaar stored encrypted at rest (given it's sensitive data)?
- [ ] **Storage overage:** What happens when 1GB limit is exceeded? Warning? Soft block?
- [ ] **Audit trail on documents:** Is there logging of who viewed/downloaded a document?
