# Item 104: Documents Module

**URL:** `https://payroll.zoho.in/#/documents/folder`  
**Navigation:** Left sidebar → "Documents"  
**Module:** Documents  
**Audit Date:** 2026-05-15

---

## Screenshots

- `screenshots/104-documents-module.png` — Documents module (All Documents view, empty)

---

## Documents Module — Structure

### Left Sidebar

```
Documents
├── All Documents          (#/documents/folder)
├── ORG FOLDER
│   └── Offer Letters      (#/documents/folder?folder_id=...&folder_name=Offer+Letters)
│   [New Org Folder button]
├── EMPLOYEE FOLDER
│   └── Personal Documents (#/documents/folder?folder_id=...&folder_name=Personal+Documents)
│   [New Employee Folder button]
│
├── Storage Limit: 1GB / 100 employees
└── Trash                  (#/documents/trash)
```

### Folder Types

| Folder Type | Description | Pre-created Folders |
|-------------|-------------|---------------------|
| Org Folder | Documents shared across the org (not employee-specific) | Offer Letters |
| Employee Folder | Documents per employee (employee-scoped) | Personal Documents |

### Sidebar Actions

| Action | Type | Behavior |
|--------|------|----------|
| New Org Folder | Button (icon) | Creates a new org-level folder |
| New Employee Folder | Button (icon) | Creates a new employee-scoped folder |
| Folder kebab (Show dropdown menu) | Button | Per-folder actions: rename, delete (inferred) |

---

## All Documents View (Main Panel)

**URL:** `#/documents/folder`

### Filter Bar

| Filter | Type | Options |
|--------|------|---------|
| Select Employee Status | Dropdown | Active / Inactive / All (inferred) |
| Select an Employee | Combobox | Employee search/select |
| Close Filter | Button | Hides filter bar |

### Upload Area

**Drag & Drop upload zone:**
- Primary action: "Drag & Drop File Here"
- Alternative: "Choose file to upload" or "Choose File" button

**Upload note:**
> "While uploading **.zip** ensure that files are of type **.pdf** and the names of the files correspond to the **ID** that person (file size should not exceed 50MB)"

**Key rules from note:**
1. Supported format: .pdf (individual) or .zip (bulk upload)
2. Bulk upload via .zip: PDF files inside must be named after employee ID
3. File size limit: 50MB per upload

---

## Document List (populated state — not observed in test org)

Expected columns (inferred from Zoho Payroll conventions):
- File Name
- Folder
- Employee Name (for employee-folder docs)
- Upload Date
- Size
- Actions: Download, Delete, Move

---

## Business Rules

1. **Two folder types:** Org Folder (shared) vs Employee Folder (per-employee scoped). Employee Folder documents are accessible to the specific employee via portal.
2. **Storage limit:** 1GB per 100 employees — scales with employee count.
3. **Trash:** Soft-delete — deleted documents go to Trash and can be recovered or permanently deleted.
4. **Bulk upload:** .zip file where each PDF is named after the employee ID — allows uploading 100+ documents at once (e.g., offer letters for all employees).
5. **Pre-built folders:** "Offer Letters" (Org) and "Personal Documents" (Employee) are auto-created on org setup.
6. **Permission-gated:** Documents permissions in RBAC: View Documents | Upload Documents | Delete Documents | Manage Folder.

---

## Data Relationships

- Document → Folder (many-to-one): each document belongs to one folder
- Employee Folder → Employee (many-to-one): scoped documents belong to specific employee
- Org Folder → Org (many-to-one): org-level documents accessible to all users with permission

---

## Cross-Module Impact

- Documents → Employee Profile: employee documents can be accessed from employee profile (inferred)
- Documents → Employee Portal: employee-scoped documents visible to employee in portal
- Documents → RBAC: permissions control who can view, upload, delete, manage folders

---

## Open Questions

- [ ] Can employees upload their own documents to Employee Folder via the portal?
- [ ] Are payslip PDFs auto-stored in Documents module or only available via Payslips & Forms tab?
- [ ] What is the naming convention for bulk .zip uploads — is it employee ID or employee number?
- [ ] Is there a document category/tag system for filtering by document type?
- [ ] Can documents be linked to specific HR events (onboarding, exit, salary revision)?
- [ ] Is there an e-sign/digital signature feature for documents (e.g., offer letter sign-off)?
