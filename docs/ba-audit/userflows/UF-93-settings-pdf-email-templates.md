# UF-93: Settings — PDF Templates and Email Templates

**Module:** Settings > Customisations > PDF Templates / Email Templates
**Tested:** 2026-05-16
**URLs:**
- PDF Templates: `#/settings/templates/regular-payslip`
- Email Templates: `#/settings/email-templates`

---

## PDF Templates Page

### URL
`#/settings/templates/regular-payslip`

### Page Structure
Left sidebar sub-navigation:
```
PDF Templates
├── payslip (group)
│   ├── Regular Payslips          → #/settings/templates/regular-payslip
│   └── Final Settlement Payslip  → #/settings/templates/final-settlement-payslip
└── Letter Templates (group)
    ├── Salary Certificate         → #/settings/templates/letter-templates/salary-certificate
    ├── Salary Revision Letter     → #/settings/templates/letter-templates/salary-revision
    └── Bonus Letter               → #/settings/templates/letter-templates/bonus-letter
```

---

## Regular Payslip Templates (7 templates)

| Template Name | Default | Actions |
|--------------|---------|---------|
| Elegant Template | YES (Default) | Preview, Edit |
| Standard Template | No | Set as Default, Preview, Edit |
| Mini Template | No | Set as Default, Preview, Edit |
| Simple Template | No | Set as Default, Preview, Edit |
| Lite Template | No | Set as Default, Preview, Edit |
| Simple Spreadsheet Template | No | Set as Default, Preview, Edit |
| Professional Template | No | Set as Default, Preview, Edit |

**Current Default:** Elegant Template

**Template ID pattern:** `#/settings/templates/regular-payslip/{templateId}/edit`
- Elegant: `3848927000000032588`
- Standard: `3848927000000032590`
- Mini: `3848927000000032592`
- Simple: `3848927000000032594`
- Lite: `3848927000000032596`
- Simple Spreadsheet: `3848927000000032598`
- Professional: `3848927000000032600`

### Template Actions
| Action | Description |
|--------|-------------|
| Set as Default | Makes this template the default for all new payslips |
| Preview | Opens a preview of the template with sample data |
| Edit | Opens template editor to customize |

### Template Editor (Expected)
- Drag-and-drop layout editor
- Configurable sections: Company logo, employee details, earnings, deductions, summary
- Font, color, spacing customization
- Field visibility toggles (show/hide specific components)
- Company logo placement

---

## Final Settlement Payslip Template

`#/settings/templates/final-settlement-payslip`

Separate template for FnF payslips. Same 7-template gallery expected.

---

## Letter Templates

### Salary Certificate
`#/settings/templates/letter-templates/salary-certificate`

Document issued to employee confirming their salary (for visa, loan applications, etc.)

**Expected fields:**
- Employee name, designation, department
- Monthly / annual CTC
- Date of joining
- Employment status
- Signature block (HR / Finance)

### Salary Revision Letter
`#/settings/templates/letter-templates/salary-revision`

Formal letter informing employee of salary revision.

**Expected fields:**
- Old CTC, New CTC
- Effective date
- Component-wise change
- Signature block

### Bonus Letter
`#/settings/templates/letter-templates/bonus-letter`

Formal letter for bonus payment communication.

---

## Email Templates Page

### URL
`#/settings/email-templates`

### Page Header
- Heading: "Email Templates"
- "Configure Sender Email Preferences" link → `#/settings/email-preference`

### Email Template List (4 templates confirmed)

| Template Name | Description | URL |
|--------------|-------------|-----|
| Payslip Notification | "This email will be sent when you pay your employees." | `?type=payslip_notification` |
| Payslip Notification (For Portal Disabled Employees Only) | For employees without portal access | `?type=payslip_notification_portal_disabled` |
| Off Cycle & One-Time Payrolls Payslip Notification | "When you pay through Off Cycle or One-Time Payrolls." | `?type=special_payroll_payslip_notification` |
| Full & Final Settlement Payslip Notification | "Once you process the employee's final settlement." | `?type=final_settlement_payslip_notification` |

### Two Payslip Templates — Important Distinction
**Payslip Notification** — for employees WITH portal access
- Portal-enabled employees receive an email with a link to view their payslip in the portal

**Payslip Notification (Portal Disabled)** — for employees WITHOUT portal access
- Portal-disabled employees receive the payslip as a PDF attachment (since they cannot log in to portal)

### Template Editor (Expected)
- HTML email editor (rich text)
- Dynamic fields/placeholders: `{employee_name}`, `{pay_period}`, `{net_pay}`, `{company_name}`
- Subject line customizable
- Preview option

### Sender Email Preferences
`#/settings/email-preference`
- Configure the "From" email address used for all Zoho Payroll emails
- Default: noreply@zohopayroll.com or org-branded email
- Custom sender domain possible (requires DNS configuration)

---

## Business Rules
1. Elegant Template is the default payslip — admin can change
2. Template changes apply to all future payslips (not retroactive)
3. Portal-enabled employees get email with link; portal-disabled get PDF attachment
4. FnF payslip uses a separate template (different content structure)
5. Letter templates generate on-demand (not auto-sent) — admin downloads and sends manually
6. Salary certificate is a compliance document (valid for bank/visa purposes)
7. Email templates support dynamic placeholders for personalization

## Gaps / Observations
- Template editor (drag-and-drop) not opened — content and customization options not captured
- Sender Email Preferences page not navigated
- Whether company logo is configurable per template not confirmed
- Letter template generation flow (how admin downloads salary certificate) not tested

## Open Questions
- [ ] Can admin create a NEW custom payslip template (or only edit the 7 provided)?
- [ ] Can different departments/locations use different payslip templates?
- [ ] Does the salary certificate template pull live data (current salary) or is it static?
- [ ] Is there a version history for email templates (can admin revert a template change)?
