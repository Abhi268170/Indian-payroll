# Documents > Upload Document

## URL / Navigation Path

- **Upload context:** Appears within a selected folder's detail view
- **URL pattern:** `#/documents/folder?folder_id={id}&folder_name={name}`
- **Trigger:** Selecting any folder from the sidebar

## Purpose

Documents the mechanism for uploading files into folders, the file format constraints, batch upload model, employee visibility settings, and folder management post-creation.

---

## Upload Interface

The upload interface appears in the main panel whenever a folder is selected (either Org or Employee folder type).

### Upload Zone Layout

```
┌──────────────────────────────────────────────┐
│  [Choose File button]                        │
│  ┌──────────────────────────────────────┐    │
│  │  Drag & Drop File Here               │    │
│  │  [cloud upload icon]                 │    │
│  │  [Choose File button]                │    │
│  │  - or -                              │    │
│  │  Choose file to upload [Choose File] │    │
│  └──────────────────────────────────────┘    │
│                                              │
│  ℹ While uploading .zip ensure that files   │
│    are of type .pdf and the names of the    │
│    files correspond to the ID that person   │
│    (file size should not exceed 50MB)       │
└──────────────────────────────────────────────┘
```

### Upload Mechanism Details

| Attribute | Value |
|-----------|-------|
| Upload method | Browser file picker OR drag-and-drop |
| Container format | ZIP file |
| File format inside ZIP | PDF only |
| File naming convention | Employee ID (the Zoho entity ID, not employee code) |
| Maximum file size | 50MB (applies to the ZIP file) |
| Batch size | Single ZIP containing multiple employee PDFs |
| Upload field name | `Choose File` (standard HTML file input) |

**Critical constraint:** Documents must be uploaded as a ZIP archive. Each PDF inside the ZIP must be named after the employee's ID. This is a batch-first upload model — there is no UI to upload a single PDF for a single employee in a point-and-click flow.

**Example ZIP structure:**
```
offer_letters.zip
├── 3848927000000032948.pdf   (Employee 1's offer letter)
├── 3848927000000032949.pdf   (Employee 2's offer letter)
└── 3848927000000032950.pdf   (Employee 3's offer letter)
```

---

## Employee Visibility Toggle

**"Show in Employee Portal" or equivalent:** Not surfaced as a visible UI toggle on the upload form itself. Employee visibility is controlled at the **folder level** via the `shared_public` flag:

- `shared_public: true` — all employees can see this folder and its documents in their portal
- `shared_public: false` + `shared_to: [...]` — only specified users/roles can view

Since the folder creation dialog does not expose this setting, the default is public (`shared_public: true`). To restrict visibility, the admin must use the folder edit flow (Edit option in the kebab menu).

---

## Folder Operations (Post-Creation)

### From Sidebar Kebab Menu

| Action | Dialog/Result |
|--------|--------------|
| Edit | Opens dialog with current Folder Name and Description pre-filled; allows rename/redescription |
| Delete | Deletes folder; behavior with existing documents not tested |

### From Folder Heading (Main Panel)

- **Edit icon** next to folder heading enables inline name edit directly in the main panel (separate from the sidebar kebab edit path).

---

## Version Control / Document Replacement

**Finding: No visible version control mechanism in the UI.**

The upload interface does not offer:
- "Replace document" option
- Version history list
- Rollback capability
- Version numbering

If a new ZIP is uploaded with the same employee ID filename, the behavior (replace vs. append) is not determinable from the UI alone. Requires API-level testing.

---

## Bulk Upload for Multiple Employees

**Finding: Bulk upload is the PRIMARY (and only) mechanism.**

The ZIP upload model is explicitly designed for bulk operations. There is no single-employee document upload path visible in the Documents module. A single-employee document might be possible via the Employee Profile (route: `people.employees.details.documents`), which has its own Documents tab — not covered in this session.

---

## Folder Creation Fields (Summary)

### New Org Folder

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Folder Name | Text input | Yes | Displayed in sidebar and folder heading |
| Description | Textarea | Yes | Admin reference only — not shown to employees |

### New Employee Folder

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Folder Name | Text input | Yes | Same as Org folder |
| Description | Textarea | Yes | |

---

## API Endpoints for Upload

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/v1/folders` | List all folders |
| POST | `/api/v1/folders` | Create new folder |
| GET | `/api/v1/folders/{id}` | Get folder details |
| PUT | `/api/v1/folders/{id}` | Update folder (edit) |
| DELETE | `/api/v1/folders/{id}` | Delete folder |
| GET | `/api/v1/documents` | List all documents |
| GET | `/api/v1/documents?folder_id={id}` | List documents in a folder (returns parent_folders, sub-folders, documents) |
| GET | `/api/v1/folders/editpage` | Get shareable users/roles for RBAC assignment |

Document upload (actual file POST) endpoint not directly observed; likely `POST /api/v1/documents` with multipart form data.

---

## Settings: Document Expiry Reminders

**Path:** Settings > Employees & Contractors → Employee Personal Documents  
**URL:** `#/settings/employee/document`

Two categories of reminders:

**For Employees:**

| Column | Pre-configured Value |
|--------|---------------------|
| Reminder Name | "Expiry Notification" |
| Reminder Schedule | "On Expiry Date" |
| Applicable Documents | "All Documents" |
| Status | Toggle (configurable) |

**For Users (HR/Admin):**

| Column | Pre-configured Value |
|--------|---------------------|
| Reminder Schedule | "On Expiry Date" |
| Applicable Documents | "All Documents" |
| Status | Toggle (configurable) |

Both sections have "Reminders before expiry date" sub-section with an "Add Reminder" button to create pre-expiry notifications (configurable lead time).

**Sample Reminder preview** available via "(View Sample Reminder)" button.

---

## Cross-Module Impact

| Module | Impact |
|--------|--------|
| Employee Portal | Employees view documents from folders where `shared_public: true` |
| Notifications/Email | Expiry reminder emails sent per schedule in Settings |
| Employee Profile | `people.employees.details.documents` shows employee-specific docs (separate context) |

---

## Key Observations for Our Build

1. **ZIP-based batch upload** is the core model. For v1, implement this exactly. Single-file upload per employee is a nice-to-have for v2 (add via Employee Profile route).

2. **File naming by employee ID** (not employee code or PAN) creates a dependency on our internal ID system. Our IDs must be stable and exportable so HR can name files correctly.

3. **50MB ZIP limit** is reasonable. MinIO can handle this. Enforce at the API level with a 400 error and clear message.

4. **No version control** in Zoho's implementation. For our build, consider adding basic versioning (keep previous file on re-upload with timestamp suffix) — this is a compliance plus for offer letters and appointment letters.

5. **Employee visibility is folder-level, not document-level** — simpler to implement but coarser-grained. For v1, mirror this. For v2, consider document-level visibility.

6. **Description field on folders is required** but only visible to admins. Consider making it optional for our implementation — forcing a description creates friction with no employee-facing benefit.

7. **RBAC sharing (shared_to) is not surfaced in creation dialog** — it's a backend-only feature in Zoho's current UI. Our implementation should surface it in the Create Folder form (which role can see this folder).

8. **The `people.employees.details.documents` route exists** — employee profile has its own documents tab. This is the right place for per-employee document management (single-file upload, view history, download). Plan this as a complementary entry point to the bulk Documents module.
