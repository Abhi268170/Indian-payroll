# Documents > Document List & Module Overview

## URL / Navigation Path

- **Primary URL:** `#/documents/folder`
- **Org-only URL:** `#/documents/organization-folder` → title "All Org Documents"
- **Employee-only URL:** `#/documents/employee-folder` → title "Employee Documents"
- **Trash:** `#/documents/trash`
- **Page title:** "All Documents | Documents | Zoho Payroll"
- **Module:** Documents (standalone, between Giving and Reports in nav)
- **Status bar message:** "Documents All Documents page loaded"

## Purpose

Central document repository for the organization. Supports two categories of documents: organization-level (accessible to all) and employee-level (tied to specific employees). Documents are uploaded in batches as ZIP archives. A left sidebar provides folder navigation; the main panel shows folder contents and an upload interface.

---

## Layout

### Overall Structure

```
[Left Sidebar: App Nav] | [Left Panel: Documents Sidebar] | [Right Panel: Entity Details]
```

### Left Panel: Documents Sidebar

**Heading:** "Documents"

**Navigation items (in order):**

1. **All Documents** link → `#/documents/folder` (shows all org + employee docs unified)
2. **Org Folder** section header
   - Button: "New Org Folder" (+ icon)
   - Folder list (empty state: "There are no folders." + "New folder" button)
   - Each folder item has: folder name link + "Show dropdown menu" button (kebab)
3. **Employee Folder** section header
   - Button: "New Employee Folder" (+ icon)
   - Folder list (empty state: "There are no folders." + "New folder" button)
   - Each folder item has: folder name link + "Show dropdown menu" button (kebab)
4. **Storage Limit** indicator: "1GB / 100 employees"
5. **Trash** link → `#/documents/trash`

### Right Panel: Entity Details

**When no folder selected (All Documents view):**
- Heading: "All Documents"
- Filter button
- Filter bar (hidden by default, shown when "Filter" clicked):
  - "Filter By:" label
  - "Select Employee Status" combobox
  - "Select an Employee" combobox
  - "Close Filter" button
- Main area: upload zone OR document table (when documents exist)

**When a folder is selected:**
- Heading: `{Folder Name}` with edit icon (pencil)
- Same filter + upload zone

---

## Folder Types

| Type | API Value | Description | Route Context |
|------|-----------|-------------|--------------|
| Org Public Folder | `org_public_folder` | Organization-wide documents (policies, offer letters) | Visible in `documents.folder` and `documents.organization-folder` |
| Payroll Employee Folder | `payroll_employee_folder` | Per-employee personal documents (KYC, ID proofs) | Visible in `documents.folder` and `documents.employee-folder` |

---

## Folder Data Model

Source: `GET /api/v1/folders/{folder_id}`

```json
{
  "folder_id": "3848927000000034272",
  "folder_name": "Personal Documents",
  "folder_type": "payroll_employee_folder",
  "description": "Personal KYC documents for employees",
  "parent_folder_id": "",
  "depth": 0,
  "shared_public": true,
  "shared_to": []
}
```

| Attribute | Type | Description |
|-----------|------|-------------|
| `folder_id` | String (18-digit Zoho ID) | Unique identifier |
| `folder_name` | String | Display name |
| `folder_type` | Enum | `org_public_folder` or `payroll_employee_folder` |
| `description` | String | Admin-provided description |
| `parent_folder_id` | String (empty = root) | Supports nested folders |
| `depth` | Integer | Nesting level (0 = root) |
| `shared_public` | Boolean | True = visible to all employees in portal |
| `shared_to` | Array | Specific user/role sharing targets |

---

## Folder Creation Forms

### New Org Folder Dialog

**Trigger:** "New Org Folder" button in sidebar  
**Type:** Modal dialog  
**Title:** "New Org Folder"

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Folder Name | Text | Yes | API field: `folder_name` |
| Description | Textarea | Yes | API field: `description` |

**Actions:** Save | Cancel

**Post-save behavior:** Folder appears immediately in sidebar under "Org Folder"; main panel switches to upload interface.

### New Employee Folder Dialog

**Trigger:** "New Employee Folder" button in sidebar  
**Type:** Modal dialog  
**Title:** "New Employee Folder"

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Folder Name | Text | Yes | Same fields as Org folder |
| Description | Textarea | Yes | |

**Actions:** Save | Cancel

**Post-save behavior:** Folder appears under "Employee Folder" section.

### RBAC Configuration

The `GET /api/v1/folders/editpage` endpoint reveals that folder permissions can be assigned to:
- Individual users (type: "user")
- Roles (type: "role": Admin = mandatory, Manager = optional)

However, the UI **does not expose role assignment during folder creation** — the form only asks for Name and Description. Role assignment may happen in a separate "Edit" flow or is auto-assigned (Admin always gets access).

---

## Folder Actions (Kebab Menu)

| Action | Description |
|--------|-------------|
| Edit | Opens edit dialog for folder name/description |
| Delete | Deletes folder (behavior if documents exist — not tested) |

---

## Filter System

**"Filter By:" bar** (shown when "Filter" button is clicked):

| Filter | Type | Options |
|--------|------|---------|
| Select Employee Status | Combobox | Active / Inactive / etc. (options not enumerated) |
| Select an Employee | Combobox | Employee search/select |

Filters apply to the document list in the main panel.

---

## Storage Limit

**Displayed in sidebar:** "1GB / 100 employees"  
**Interpretation:** The storage quota scales with employee count. At 100 employees the org gets 1GB total document storage.  
**Current usage:** Not displayed as a progress bar (shows only the limit, not current used).

---

## Three Route Contexts

The Documents module has three distinct top-level route contexts, each with their own secondary sidebar:

| Route | Title | Secondary Sidebar Content |
|-------|-------|--------------------------|
| `documents.folder` | All Documents | All Org + Employee folders unified |
| `documents.organization-folder` | All Org Documents | Only Org folders; sub-nav: "All Org Documents" → `…/details`, Trash → `…/trash` |
| `documents.employee-folder` | Employee Documents | Only Employee folders; sub-nav: "All Employee Documents" → `…/details`, folder list, Trash → `…/trash` |

---

## Empty State Messaging

| Context | Empty State Message |
|---------|-------------------|
| No folders exist | "You have not created any folders yet to upload documents" |
| Org Folder section, no folders | "There are no folders." + "New folder" button |
| Employee Folder section, no folders | "There are no folders." + "New folder" button |

---

## Navigation Paths

**Entry points:**
- Sidebar: "Documents" link
- Direct URL: `#/documents/folder`

**Exit points:**
- Any sidebar module
- Folder detail view: `#/documents/folder?folder_id={id}&folder_name={name}`
- Org-only view: `#/documents/organization-folder`
- Employee-only view: `#/documents/employee-folder`
- Trash: `#/documents/trash`

---

## Key Observations for Our Build

1. **Three separate route contexts** (unified, org-only, employee-only) suggest the module is designed to be embedded in different contexts (e.g., employee profile page shows only that employee's documents, while HR admin sees all). Our implementation should follow this separation.

2. **Folder nesting is supported** by the data model (`parent_folder_id`, `depth`) but the UI appears to be flat (no sub-folders shown). We can implement hierarchical folders but should default to flat for v1.

3. **Shared public by default** — `shared_public: true` on created folders means all employees can see the folder. RBAC tightening happens via `shared_to` array. This is the right default for offer letters but wrong default for sensitive HR documents.

4. **Storage limit is employee-count-based** — 1GB per 100 employees. Our MinIO implementation should enforce this quota per tenant.

5. **Separate route for org vs employee documents** is important for access control — HR admin sees all, managers see their team, employees see only their own.

6. **Edit icon on folder heading** — suggests inline editing of folder name is possible from the detail view (not just from the sidebar kebab menu).
