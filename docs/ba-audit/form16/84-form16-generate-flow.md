# Form 16 > Generate Flow — Prerequisites and Step-by-Step

## URL / Navigation Path
- Taxes & Forms > Form 16: `#/taxes-and-forms/form16`
- FY-scoped: `#/taxes-and-forms/form16?fiscal_year=2026`

## Purpose
Documents the complete Form 16 generation workflow: gate checks that must pass before generation begins, the 4-step generation process, and the status transitions for each employee record.

## Prerequisites (Gate Checks)

Form 16 generation is guarded by a sequential set of prerequisites. All must be satisfied before the "Generate Form 16" action becomes available.

### Gate 1: Tax Deductor Configured (HARD GATE — UI-enforced)

**What is required:**
| Field | Source |
|-------|--------|
| Employer TAN | Settings > Tax Details > TAN |
| Employer PAN | Settings > Tax Details > Employer PAN |
| Tax Deductor Name | Settings > Tax Details > Deductor section |
| Tax Deductor PAN | Settings > Tax Details > Deductor section |

**UI behaviour when not satisfied:**
- Alert shown: "Tax Deductor is not found"
- Link shown: "Add Tax Deductor" → `#/settings/taxes`
- Warning: "Remember that once you generate Form 16, you cannot change the deductor details."
- All generation buttons absent; only instructional content visible

**Invariant (post-generation):** Once Form 16 generation begins for a given financial year, the deductor record becomes immutable for that FY. The system must prevent edits to Tax Deductor fields that would affect already-generated Form 16s.

### Gate 2: Form 24Q Filed for All Quarters (SOFT GATE — process dependency)

**What is required:**
- Form 24Q must be filed with TRACES for all 4 quarters of the financial year (Q1–Q4)
- TRACES must have processed and accepted the Form 24Q submissions
- Without this, Part A is not available on TRACES for download

**Why this is a soft gate:** Zoho does not check TRACES directly. The admin is responsible for downloading Part A from TRACES and uploading it. Zoho's gate is the Part A upload — if Part A is not uploaded, Form 16 cannot be generated. The Form 24Q dependency is external to Zoho.

### Gate 3: Part A Uploaded (PROCESS GATE — per employee)

**What is required:**
- Admin downloads Part A from TRACES (https://www.tdscpc.gov.in) after Form 24Q acceptance
- Admin uploads Part A file to Zoho Form 16 interface
- Zoho matches Part A to employees by PAN
- Each employee must have a matching Part A entry

**Per-employee PAN validation:**
- If employee PAN is missing → Part A cannot be matched → Form 16 cannot be generated for that employee
- Employees without PAN are excluded from Form 16 generation (must add PAN first)

### Gate 4: Employee PAN Present (PER-EMPLOYEE GATE)

- Every employee for whom Form 16 is generated must have PAN in their profile
- Employees without PAN: blocked from Form 16 generation
- UI should show a validation warning per employee row

## 4-Step Generation Flow

### Step 1: Upload Form 16 Part A

**Trigger:** Admin navigates to Taxes & Forms > Form 16, selects FY, clicks "Upload Part A"

**Process:**
1. File picker opens — admin selects the TRACES-downloaded Part A file
2. File format: Digitally-signed PDF from TRACES (Part A certificate per employee, or batch file)
3. Zoho parses the uploaded file and extracts employee PAN records
4. System matches each Part A record to an employee by PAN
5. Unmatched PANs: flagged — admin must resolve (check if employee PAN is correct)
6. Matched employees: status transitions to "Part A Uploaded"

**Expected file upload component:**
| Property | Details |
|----------|---------|
| Input type | File upload (drag-and-drop or click-to-browse) |
| Accepted formats | PDF (TRACES-issued Part A certificate) |
| Validation | File must be a TRACES-format Part A; invalid files rejected |
| Matching key | Employee PAN (TRACES Part A PAN matched to employee profile PAN) |
| Error handling | Unmatched PAN shown in error list; admin must resolve |

**Post-upload state:** Per employee — "Part A Uploaded" or "Error: PAN not matched"

### Step 2: Generate Form 16

**Trigger:** Admin clicks "Generate Form 16" (bulk or per-employee)

**Process:**
1. Zoho fetches all approved pay runs for the FY for each employee
2. Zoho generates Part B (salary breakup, deductions, tax computation — see 81-form16-part-b.md)
3. Part A (uploaded from TRACES) is combined with generated Part B
4. Merged PDF created per employee
5. PDF is password-protected (employee PAN as default password)
6. Status transitions to "Generated"

**Bulk vs individual generation:**
- Bulk: Generate for all employees who have Part A uploaded
- Individual: Generate per employee (row-level action)

**Preconditions per employee for generation:**
- Part A status = "Part A Uploaded"
- All pay runs for the FY completed (no draft/pending runs)
- Employee PAN present in profile

**Post-generation state:** Status = "Generated"

**Note on re-generation:** Whether Zoho allows re-generation after initial generation is unclear from the blocked state. Standard approach: re-generation should be restricted or require admin confirmation as it produces a revised document. The "deductor details cannot be changed after generation" note implies generation is a significant, semi-irreversible step.

### Step 3: Sign Form 16

**Trigger:** Admin selects digital signature method and applies to generated Form 16s

**Methods available (Zoho):**
| Method | Description | Requirements |
|--------|-------------|-------------|
| DSC (Digital Signature Certificate) | PKI-based hardware token | Admin must upload/configure DSC in Zoho; Class 2 or Class 3 DSC |
| e-Sign (Aadhaar-based) | Electronic signature via UIDAI/e-Sign gateway | Admin's Aadhaar OTP authentication; requires e-Sign service integration |
| Unsigned | Distribute without digital signature | Legally permitted but weaker; CBDT recommends DSC/e-Sign for electronic Form 16 |

**Process (DSC):**
1. Admin configures DSC in Zoho settings (hardware token or uploaded certificate)
2. Admin selects Form 16 records to sign
3. Zoho applies DSC to each PDF
4. Status transitions to "Signed"

**Process (e-Sign):**
1. Admin triggers e-Sign for selected Form 16s
2. Aadhaar OTP sent to admin's registered mobile
3. Admin enters OTP
4. e-Sign gateway (NSDL/NPCI-integrated) applies electronic signature
5. Status transitions to "Signed"

**CBDT requirement:** Digital signature on electronically issued Form 16 is mandatory per CBDT notification. DSC or e-Sign satisfies this requirement. Unsigned distribution is non-compliant if issued electronically.

**Post-signing state:** Status = "Signed"

### Step 4: Publish / Email

**Trigger:** Admin clicks "Publish" or "Email" (can be done together or separately)

**Publish (Employee Self-Service Portal):**
1. Signed Form 16 PDF made visible in employee's self-service portal
2. Employee can download from "Payslips & Forms" tab
3. Password: employee PAN (employee must know their PAN to open the PDF)
4. Status transitions to "Published"

**Email:**
1. Zoho sends email to each employee's registered email address
2. Attachment: password-protected Form 16 PDF
3. Email body informs employee that password = their PAN
4. Subject: typically "Form 16 for FY YYYY-YY — [Org Name]"
5. Status transitions to "Emailed" (may combine with "Published")

**Bulk operations:**
- Bulk Publish: publish all "Signed" Form 16s at once
- Bulk Email: email all "Signed" Form 16s at once
- Bulk Download ZIP: download all Form 16 PDFs as ZIP archive (admin archive/records)

## Status State Machine

```
Not Generated
     │
     │ [Part A Uploaded]
     ▼
Part A Uploaded
     │
     │ [Generate Form 16]
     ▼
Generated
     │
     │ [Apply Digital Signature]
     ▼
Signed
     │
     │ [Publish to portal] ──── [Email to employee]
     ▼                              ▼
Published                        Emailed
```

**Notes:**
- "Signed" step can be skipped (unsigned distribution) → goes directly to Published/Emailed
- "Published" and "Emailed" are not mutually exclusive — both can apply
- Status regression is not expected (no "Un-generate" or "Un-publish" action observed)

## Financial Year Scoping

- Form 16 is FY-scoped (one Form 16 per employee per FY)
- FY selector in page header determines which year's Form 16s are being managed
- Assessment Year = FY + 1 (e.g., FY2025-26 → AY2026-27)
- Generation for prior FYs: possible if records exist, but pay runs for that FY must be finalised

## Statutory Deadlines

| Deadline | Reference | Notes |
|----------|-----------|-------|
| 31 May (after end of FY) | Rule 31, IT Act | Form 16 must be issued to employees by this date |
| FY2025-26 Form 16 | Deadline: 31/05/2026 | |
| FY2026-27 Form 16 | Deadline: 31/05/2027 | |

## Cross-Module Dependencies

| Module | Dependency |
|--------|------------|
| Settings > Tax Details | TAN, PAN, Deductor — mandatory before generation |
| Taxes & Forms > Form 24Q | All quarters filed before TRACES Part A is available |
| Taxes & Forms > Challans | Challan data feeds Form 24Q |
| Pay Runs (all months) | Part B data source; all runs must be completed |
| Employee Profile > Personal Info | PAN — per employee gate |
| IT Declaration | Employee-declared deductions for Part B (old regime) |
| Proof of Investment | Verified investment amounts for Part B |
| Prior Employer YTD | Mid-year joiners — combined YTD in Part B |
| Settings > Email Templates | Form 16 email template customisation |

## Key Observations for Our Build

1. **Generation is FY-scoped, per-employee** — build `Form16Record` entity with `(TenantId, EmployeeId, FinancialYear)` composite key.

2. **Deductor immutability** — once any Form16Record for a FY reaches `Generated` status, the TaxDeductor entity for that FY should be locked. Implement as a `IsLockedForFY(int year)` check before allowing deductor edits.

3. **Part A upload endpoint** — accept TRACES PDF, parse employee PAN from document, match to employee records. Store Part A file in MinIO per employee per FY. This is the most complex technical piece.

4. **PAN validation gate** — before allowing Form 16 generation for an employee, check `Employee.PAN != null`. Surface per-employee validation errors in bulk generation UI.

5. **PDF generation service** — combine Part A PDF + Part B content into single PDF with AES encryption (PAN as password). Libraries: iText 7 (paid) or PdfSharpCore (LGPL). Part A content must not be altered — append as pages.

6. **DSC for v1: consider deferred** — DSC integration requires PKI middleware (hardware token bridge). Aadhaar e-Sign requires NSDL e-Sign API integration. For v1, implement unsigned distribution; mark DSC/e-Sign as `// DEFERRED: digital-signature`.

7. **Bulk operations** — implement as background jobs (Hangfire). Form 16 generation for 500 employees in bulk cannot be synchronous. Use job queue with progress tracking.

8. **Compliance calendar entry** — auto-create a compliance calendar task for Form 16 issuance deadline (31 May) when a new FY begins.

9. **Status as enum** — `Form16Status` enum: `NotGenerated = 0`, `PartAUploaded = 1`, `Generated = 2`, `Signed = 3`, `Published = 4`, `Emailed = 5`. Status is monotonically increasing (no regression).

10. **All pay runs must be finalised** — before allowing Form 16 generation, validate that no draft or in-progress pay runs exist for the FY for that employee. Surface clear error: "Payroll for [Month] is not finalised. Please finalise before generating Form 16."

## Screenshots
- `screenshots/form16-landing.png` — Form 16 landing page showing Tax Deductor gate and 4-step instructional flow
