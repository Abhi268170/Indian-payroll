# Settings > Data Backup

## URL
`#/settings/data-backup`

## Navigation Location
Settings > EXTENSIONS & DEVELOPER DATA > Developer Data > Data Backup

## Sub-tabs

| Tab | URL |
|-----|-----|
| Backup Data | `#/settings/data-backup` |
| Backup Audit Trail | `#/settings/data-backup/audit-trail-backup` |

---

## Sub-tab 1: Backup Data (`#/settings/data-backup`)

### Purpose
Generate a complete export of all org data in CSV format, delivered via email.

### Description
> "Get a backup copy of the data in your Zoho Payroll organisation sent to your email as a CSV file. Click here to view a list of all the modules included in this data backup."

**"Click here to view" link** — opens a pop-up or page listing which modules are included in the backup (content not captured).

### Actions
| Button | Action |
|--------|--------|
| Back Up Data | Triggers backup generation; sends download link to admin email |
| Click here to view | Lists modules included in backup |

### Backup History Table
| Column | Description |
|--------|-------------|
| Backup Time | Timestamp of when backup was initiated |
| User Name | Who triggered the backup |
| File Type | Format (CSV — inferred) |
| Export Status | Processing / Complete / Failed |
| Download Link | Link to download the ZIP file |

**Current state:** "You've not made any back-ups yet."

---

## Sub-tab 2: Backup Audit Trail (`#/settings/data-backup/audit-trail-backup`)

### Purpose
Backup the application's audit trail (all user activity logs) as a ZIP file sent via email.

### Description
> "Zoho Payroll backs up your Audit Trail activity into a ZIP file and emails you a download link. The link includes only the activity between the current and previous backup dates."

**Key behaviour:** Incremental audit trail — each backup contains only NEW activity since the last backup.

### Actions
| Button | Action |
|--------|--------|
| Back up your Audit Trail | Triggers audit trail backup; sends download link |

### Backup History Table
Same structure as Backup Data:
| Column | Notes |
|--------|-------|
| Backup Time | When backup was initiated |
| User Name | Who triggered it |
| File Type | ZIP containing activity logs |
| Export Status | Processing / Complete / Failed |
| Download Link | Link to download the ZIP |

**Current state:** "You've not made any back-ups yet."

---

## Business Rules

1. **On-demand backup** — no scheduled/automatic backup visible; admin must manually trigger.
2. **Email delivery** — backup download link sent to admin's email, not available as direct download from UI.
3. **Incremental audit trail** — each audit backup covers new activity since last backup (delta, not full). Admins should back up regularly.
4. **CSV format for data** — entire org data exportable as CSV (all modules).
5. **ZIP format for audit trail** — audit activity compressed into ZIP.

## Observations & Notes
1. **No scheduled backup** — no option for daily/weekly automatic backup. Risk: admin may forget to back up.
2. **Email delivery** — download link in email (not in-UI download) adds friction; link may expire.
3. **Audit trail incremental** — important for compliance. Advisable to back up monthly before clearing logs.
4. **Scope of "all modules" not confirmed** — "Click here to view" link content not captured; unclear if it includes PF ECR data, TDS returns, payslip data, etc.
5. For our build: Automated daily backup to MinIO (S3-compatible). Admin-triggered full export as ZIP (CSV per entity). Audit trail: separate append-only log table, exportable as CSV. GDPR/DPDP compliance: allow full data export for data portability.

## Screenshots
`docs/ba-audit/settings/screenshots/33-data-backup.png`
