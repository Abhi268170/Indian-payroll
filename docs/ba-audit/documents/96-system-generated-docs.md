# Documents > System-Generated Documents

## Purpose

Documents whether Zoho Payroll auto-populates the Documents module with system-generated documents like payslips and Form 16, and what the document data model looks like.

---

## Payslips in Documents Module

**Finding: Payslips are NOT stored in the Documents module.**

The Documents API (`GET /api/v1/documents`) does not contain payslips. The `applied_filter: "Type.All"` filter returns zero results even when a payroll run for May 2025 has been completed.

Payslips are managed through a completely separate subsystem:
- Payslips are accessed via Pay Runs → individual payrun → payslip generation
- Distribution is via email (Send Payslip action)
- Download is from the Pay Run detail view or Employee Portal
- The Documents module does NOT aggregate payslips

**Implication for our build:** Do not store system-generated payslips in the same document storage layer as user-uploaded files. Maintain separate storage with separate access controls.

---

## Form 16 in Documents Module

**Finding: Form 16 is NOT stored in the Documents module.**

Form 16 is managed through the Taxes & Forms → Form 16 module. It is not accessible through `GET /api/v1/documents`.

---

## Document API Data Model

Source: `GET /api/v1/documents`

**Response structure:**
```json
{
  "code": 0,
  "message": "success",
  "documents": [],
  "page_context": {
    "page": 1,
    "per_page": 100,
    "has_more_page": false,
    "applied_filter": "Type.All",
    "sort_column": "created_time",
    "sort_order": "A"
  }
}
```

**Folder-scoped documents:** `GET /api/v1/documents?folder_id={id}`

Response adds additional fields:
```json
{
  "parent_folders": [],
  "folders": [],
  "documents": []
}
```

This suggests that the API returns both sub-folders and documents within a folder, enabling a tree-like navigation.

**Default sort:** `created_time` ascending.  
**Default page size:** 100 documents per page.  
**Pagination:** Supported via `page` and `per_page` params.

---

## Document Entities in the System

Based on all route analysis and API investigation, the Documents module manages only **user-uploaded** files. System-generated documents (payslips, Form 16, ECR files, challans) are each managed within their respective modules:

| Document Type | Location in App | Access Method |
|--------------|-----------------|--------------|
| Payslips | Pay Runs → Pay Run Detail | Download per employee; email distribution |
| Form 16 | Taxes & Forms → Form 16 | Download after generation |
| Form 24Q | Taxes & Forms → Form 24Q | Download per quarter |
| ECR File | Not yet audited (PF module) | TBD |
| TDS Challans | Taxes & Forms → Challans | Download per challan |
| Custom Documents | Documents module | Admin upload; employee view via portal |

---

## Folder Document Listing (Populated State)

Not directly observable (no documents uploaded). From the API response structure, a populated documents list within a folder would include:

| Field | Expected Type | Notes |
|-------|--------------|-------|
| `document_id` | String | Zoho entity ID |
| `document_name` | String | File name displayed |
| `folder_id` | String | Parent folder |
| `employee_id` | String | For employee-folder docs, linked employee |
| `file_size` | Integer | Bytes |
| `created_time` | Timestamp | Upload timestamp |
| `uploaded_by` | String | User who uploaded |
| `expiry_date` | Date | Optional; drives expiry reminders |
| `file_type` | String | Expected: `pdf` |

*(Fields inferred from API response structure and Settings expiry configuration; exact field names unconfirmed without an actual document upload.)*

---

## Document Expiry System

**Settings path:** `#/settings/employee/document` → "Employee Personal Documents"

The Settings module configures email reminders for document expiry:

### Expiry Reminder for Employees

| Column | Description |
|--------|-------------|
| Reminder Name | Descriptive label (e.g., "Expiry Notification") |
| Reminder Schedule | When email is sent (e.g., "On Expiry Date", "X days before expiry") |
| Applicable Documents | Document types this reminder covers (e.g., "All Documents") |
| Status | Toggle (enabled/disabled) |
| Actions | Edit icon |

**Pre-configured reminders:**
- "Expiry Notification" — On Expiry Date — All Documents (status: configurable)

**Section:** "Reminders before expiry date" — empty by default, "Add Reminder" button to create pre-expiry reminders.

### Expiry Reminder for Users (Admin/HR)

Separate reminder set that notifies HR users (not employees) about expiring documents. Auto-generated; notifies all users with "view access for the personal document module."

**Note text:** "The Reminder will be auto generated and it will notify all the user who have view access for the personal document module."

Has a "(View Sample Reminder)" button for preview.

---

## Key Observations for Our Build

1. **Payslips and Form 16 are entirely separate from Documents** — do not cross-link these systems. Each has its own storage, generation, and distribution mechanism.

2. **Document expiry is a core feature** — not just a "nice to have." Settings has a fully built-out notification scheduler for expiry reminders. Our document entity should include an `expiry_date` field and a reminder scheduling system.

3. **Two audience types for expiry reminders** — employees (about their own docs) and HR users (about any doc). Our notification system must support role-based recipient lists.

4. **"Add Reminder" with configurable schedule** — the reminder system is not just a single trigger. It supports:
   - On expiry date
   - X days before expiry (configurable, multiple allowed)
   This requires a scheduled job system (Hangfire in our stack is appropriate).

5. **System-generated vs user-uploaded documents should NOT share a table** — use separate database relations and storage prefixes. This avoids permission contamination and makes purging/archiving simpler.
