# Form 16 > Employee Self-Service Portal View

## URL / Navigation Path

**Admin view (employer):**
- Taxes & Forms > Form 16: `#/taxes-and-forms/form16`

**Employee self-service view:**
- Employee portal > Payslips & Forms tab (route TBD — employee-facing portal has separate URL from admin)
- Within employee profile (admin view): Employee > [Employee Name] > Payslips & Forms tab

## Purpose

Documents what an employee sees when they access their Form 16 through the self-service portal, how they download it, password protection, and the employer-side configuration that controls visibility.

## Employee Self-Service Portal Access

### Entry Point

Employees access Form 16 via their self-service portal:
- Portal URL: typically `payroll.zoho.in/portal` or org-subdomain employee login
- Navigation: Payslips & Forms (tab or section in employee dashboard)

### Availability Trigger

Form 16 becomes visible to employees **only after the admin publishes** it:
- Admin must complete Step 4 (Publish/Email) in the Form 16 generation flow
- Pre-publish: Form 16 is not visible in employee portal (even if Generated or Signed)
- Post-publish: Form 16 appears in employee's Payslips & Forms section

### What Employee Sees (Expected, based on Zoho pattern)

| Element | Details |
|---------|---------|
| Section heading | "Form 16" or "Tax Documents" |
| Financial Year label | "Form 16 — FY 2025-26" |
| Download button | "Download Form 16" — triggers PDF download |
| Status indicator | None (employee sees only published documents) |
| Year selector | If multiple FYs available, dropdown to select year |

### Per-FY Record

One Form 16 per financial year. If the employee has been with the org for multiple years, one record per FY appears in the list, ordered by FY descending.

## PDF Download Mechanics

### Password Protection

**Standard practice (confirmed for Zoho):** Form 16 PDF is password-protected using employee's PAN as the password.

| Property | Details |
|----------|---------|
| Password | Employee's PAN (e.g., `ABCPM1234A`) |
| Encryption | AES-256 (PDF encryption standard) |
| Communication to employee | Email body informs employee that password = their PAN |
| Portal display | Password hint shown in portal UI (e.g., "Open with your PAN as password") |

**Rationale for PAN as password:**
- PAN is unique per employee
- Employee always knows their own PAN
- Avoids need for separate password communication
- Standard industry practice for Form 16 distribution in India

### Download Format

- Single merged PDF per employee per FY
- Contains Part A (TRACES-sourced) + Part B (employer-generated)
- File naming convention (expected): `Form16_[PAN]_FY[YYYY-YY].pdf` (e.g., `Form16_ABCPM1234A_FY2025-26.pdf`)
- If digitally signed: DSC or e-Sign signature embedded in PDF metadata

## Admin View of Employee Form 16

From the admin's perspective, navigating to an employee's profile shows their Form 16 history:

**Navigation:** Employees > [Employee Name] > Payslips & Forms tab

| Column | Details |
|--------|---------|
| Document Type | "Form 16" |
| Financial Year | "FY 2025-26" |
| Status | Published / Emailed |
| Download | Admin can download employee's Form 16 |
| Email | Admin can re-send Form 16 email to employee |

## Email Distribution (Alternative/Supplementary Access)

If admin chose "Email" in Step 4:
- Employee receives email with Form 16 PDF attached
- Email includes password instructions (PAN)
- Employee can open PDF from email without logging into portal

**Both channels can be used simultaneously:**
- Publish to portal AND email — employee gets both portal access and email attachment

## State Visibility Rules

| Status | Visible in Employee Portal? | Notes |
|--------|---------------------------|-------|
| Not Generated | No | |
| Part A Uploaded | No | |
| Generated | No | Admin can see; employee cannot |
| Signed | No | Admin can see; employee cannot |
| Published | Yes | Employee can download |
| Emailed | Yes (if also Published) | Email attachment is separate; portal visibility requires Published status |

## Prior Year Form 16s

Employees who join mid-year and have prior employer Form 16s — those are NOT in Zoho. Zoho only shows Form 16s generated within the system. Prior employer Form 16s are the employee's personal documents.

For mid-year joiners (e.g., Vikram Nair joined month 6):
- Zoho generates Form 16 for that FY covering only the period employed under this org
- Part B includes only salary paid by this employer
- Prior employer TDS is shown in Form 24Q/Part A via challan records if the employer has prior employer TDS details
- **Employee must obtain prior employer Form 16 from their previous employer separately**

## Mobile Access

Zoho Payroll has a mobile app. Form 16 download from mobile:
- Employee logs into mobile app
- Payslips & Forms section available
- Form 16 PDF viewable/downloadable on mobile
- Password entry required on mobile PDF viewer (PAN)

## Statutory Context

- Rule 31 mandates employer to issue Form 16 by 31 May after end of FY
- "Issue" includes making available electronically (portal/email)
- Employee has the right to receive Form 16 from employer
- If employer fails to issue: employee can complain to IT Department

## Cross-Module Dependencies

| Module | Dependency |
|--------|------------|
| Employee Profile > Personal Info | PAN (required for PDF password; also for Part A matching) |
| Employee Profile > Personal Info | Email address (for email distribution) |
| Form 16 Generation (Step 4) | Publish action controls portal visibility |
| Settings > Email Templates | Customise Form 16 email body/subject |
| Taxes & Forms > Form 16 (admin) | Source of generated Form 16 records |

## Key Observations for Our Build

1. **Publish flag on Form16Record** — add `IsPublished` boolean (or use `Form16Status >= Published`) to control employee portal visibility. Employee API endpoint must filter by this flag.

2. **Separate employee-facing endpoint** — `GET /api/me/form16` returns only published Form 16 records for the authenticated employee. Never expose Generated/Signed records to employee.

3. **PDF password = employee PAN** — implement PDF encryption at generation time. If PAN changes (edge case), the pre-generated PDF password remains the PAN at time of generation. Store PAN snapshot at generation time.

4. **Email template** — build Form 16 email template with password instruction: "Your Form 16 is attached. Open it using your PAN as the password."

5. **File naming** — standardise file name: `Form16_{PAN}_{FY}.pdf` for both portal download and email attachment.

6. **Admin re-send capability** — allow admin to re-trigger email send per employee (resend button on employee Form 16 row). Does not change status if already Emailed; just re-sends.

7. **Year selector in portal** — employee portal Form 16 section should show one record per FY with year label. Default to most recent FY.

8. **RBAC** — employee can only see their own Form 16. HR Admin can see all employees' Form 16s. Payroll Admin can generate/publish. Role separation must be enforced at API level.

9. **MinIO storage** — store generated Form 16 PDF in MinIO with path: `form16/{tenant_id}/{employee_id}/{fiscal_year}/form16.pdf`. Access via signed URL (time-limited) for download.

10. **Audit log** — log every Form 16 download event (who downloaded, when, employee ID, FY) for compliance audit trail.

## Screenshots
- `screenshots/form16-landing.png` — Admin Form 16 list (blocked state shown; employee view not directly accessible in test state)
