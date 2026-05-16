# UF-76: Form 16 — Publish and Email to Employees

**Module:** Taxes & Forms > Form 16 > Step 4
**Tested:** 2026-05-16
**Mock Data Used:** Demo org; Form 16 not generated (Tax Deductor missing)
**App State Before:** Form 16 page; Step 4 (Publish/Email) disabled — Steps 1–3 not complete

## Steps Executed
1. Observed Form 16 page Step 4 in disabled state
2. Documented expected publish and email flow from UI structure

---

## Publish Step (Step 4a) — Expected Flow

### Pre-conditions
- Steps 1 (Upload Part A), 2 (Generate), and 3 (Sign) all complete
- Form 16 PDFs are generated and signed

### Publish Action
1. Admin clicks "Publish" on Form 16 page
2. System makes Form 16 available in employee self-service portal
3. Employee can log into portal and download their Form 16 PDF
4. Publication is per financial year — all employees published together or individually
5. Published status shows in the Form 16 page (count: N employees published)

### Portal Availability
- Employee navigates to their portal → Documents / Tax section
- Form 16 appears as a downloadable PDF
- Employee can download and share with tax filing software / CA

---

## Email Step (Step 4b) — Expected Flow

### Email Distribution
1. Admin clicks "Email to Employees" (or it may be combined with Publish)
2. System sends Form 16 PDF to each employee's registered email address
3. Email content:
   - Subject: "Your Form 16 for FY2026-27 — [Company Name]"
   - Body: Brief message from employer
   - Attachment: Form 16 PDF (password-protected with DOB DDMMYYYY or PAN?)
4. Admin sees send status: Sent / Failed per employee

### Email Security
Best practice (not confirmed in UI): Form 16 PDFs should be password-protected.
Common password patterns:
- PAN (first 5 chars) + DOB (DDMMYYYY) — e.g., ABCDE01011985
- DOB in DDMMYYYY format
- Zoho's default if any not confirmed

---

## Employee Self-Service Portal Access
(See also UF-84 through UF-88 for full portal investigation)

After Form 16 is published:
- Employee receives email OR logs into portal
- Portal URL: Separate from admin console (e.g., `payroll-portal.zoho.in` or subdomain)
- Employee authenticates with Zoho credentials (if invited)
- Form 16 visible under "Tax Documents" or "Documents" section
- Download as PDF

---

## Form 16 Revision Scenario

If TDS was incorrect and a revised Form 24Q is filed:
1. Admin re-uploads Part A (revised from TRACES)
2. Regenerates Part B
3. Re-signs revised Form 16
4. Re-publishes — overwrites previous version in portal
5. Re-emails to affected employees with note about revision

---

## Statutory Context

| Item | Details |
|------|---------|
| Issue deadline | 15th June after FY end (Section 203) |
| Penalty for late issue | ₹100/day per employee (Section 272A) |
| Employee must receive | Both Part A (from TRACES) + Part B (employer generated) |
| Digital delivery | Legally valid if sent to registered email |
| Physical copy | Not required if digitally delivered and employee consents |

---

## Cross-Module Effects
- Form 16 requires: TDS Liabilities (deposit confirmed) + Form 24Q (filed) + TRACES Part A (uploaded) + Pay Run data (salary breakup for Part B)
- Employee portal (UF-84): Form 16 visible under tax documents after publish step
- IT Declaration (UF-26): Chapter VI-A deductions appear in Part B — must be finalized before Form 16 generation

## Gaps / Observations
- Full publish/email flow not tested — Step 4 disabled due to upstream steps incomplete
- Password protection of Form 16 PDF not confirmed
- Bulk email failure handling not visible (what if employee email bounces?)
- No confirmation dialog observed (may exist before bulk email send)

## Open Questions
- [ ] Are Form 16 PDFs password-protected? What is the password format?
- [ ] Can admin selectively publish for individual employees (e.g., only for resigned employees who need it early)?
- [ ] Is there an audit log of when Form 16 was emailed to which employee?
- [ ] Can employees access Form 16 for prior financial years in the portal?
