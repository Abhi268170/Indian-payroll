# Settings > Organisation Profile

## URL
`#/settings/orgprofile`

## Purpose
Captures the company's legal identity, registered address, contact information, and display preferences (date format, field separator) used across all payroll documents, forms, and payslips.

## Page Layout
Single scrollable form with four logical sections:
1. **Logo + Basic Info** — logo upload, org name, business location, industry, date format, field separator
2. **Organisation Address** — primary work location address
3. **Filing Address** — registered address used in statutory forms (read-only display with a Change link)
4. **Contact Information** — primary contact email and sender email configuration

Organisation ID (read-only) is displayed in the page header: `Organisation ID: 60071806579`

## Fields

| Field | Type | Required | Default / Current Value | Options / Format | Help Text | Validation |
|-------|------|----------|------------------------|------------------|-----------|------------|
| Organisation Logo | File upload | Optional | None | PNG, JPG, JPEG; max 1 MB; preferred 240x240px @ 72 DPI | "This logo will be displayed on documents such as Payslip and TDS Worksheet." | File type + size enforced |
| Organisation Name | Text | Yes | lerno | Free text | "This is your registered business name which will appear in all the forms and payslips." | Non-empty |
| Business Location | Text (disabled) | Yes | India | Read-only — hardcoded to India | None | Not editable |
| Industry | Dropdown | Yes | Technology | See Industry Options below | None | Must select |
| Date Format | Dropdown | Yes | dd/MM/yyyy [ 15/05/2026 ] | See Date Format Options below | None | Must select |
| Field Separator | Dropdown | No | / | `.` , `-` , `/` | None | None |
| Address Line 1 | Text | Yes | lerno | Free text | None | Non-empty |
| Address Line 2 | Text | No | kazhakoottam | Free text | None | None |
| State | Dropdown | Yes | Kerala | All 37 Indian states/UTs (see State Options below) | None | Must select |
| City | Text | Yes | thiruvananthapuram | Free text | None | Non-empty |
| PIN Code | Text | Yes | 695010 | 6-digit numeric | None | 6-digit numeric |
| Filing Address | Read-only display | N/A | Head Office: lerno, kazhakoottam, thiruvananthapuram, Kerala 695010 | Populated from org address; changed via separate link | "This registered address will be used across all Forms and Payslips." | N/A |
| Primary Contact Email Address | Read-only display | N/A | abhijithss2255@gmail.com | Inherited from Zoho account | "This email address receives reminders and email notifications from Zoho Payroll." | N/A |
| Emails Are Sent Through | Read-only display | N/A | message-service@mail.zohopayroll.in | Configurable via Sender Email Preferences | "You can configure the email addresses used in the sender address field." | N/A |

### Industry Options (28 options)
Agency or Sales House, Agriculture, Art and Design, Automotive, Construction, Consulting, Consumer Packaged Goods, Education, Engineering, Entertainment, Financial Services, Food Services (Restaurants/Fast Food), Gaming, Government, Health Care, Interior Design, Internal, Legal, Manufacturing, Marketing, Mining and Logistics, Non-Profit, Publishing and Web Media, Real Estate, Retail (E-Commerce and Offline), Services, Technology, Telecommunications, Travel/Hospitality, Web Designing, Web Development, Writers

### Date Format Options (15 options)
- `MM/dd/yy` [ 05/15/26 ]
- `dd/MM/yy` [ 15/05/26 ]
- `yy/MM/dd` [ 26/05/15 ]
- `MM/dd/yyyy` [ 05/15/2026 ]
- `dd/MM/yyyy` [ 15/05/2026 ] ← current default
- `yyyy/MM/dd` [ 2026/05/15 ]
- `dd MMM yyyy` [ 15 May 2026 ]
- `dd MMMM yyyy` [ 15 May 2026 ]
- `MMMM dd, yyyy` [ May 15, 2026 ]
- `EEE, MMMM dd, yyyy` [ Fri, May 15, 2026 ]
- `EEEEEE, MMMM dd, yyyy` [ Friday, May 15, 2026 ]
- `MMM dd, yyyy` [ May 15, 2026 ]
- `yyyy MM dd` [ 2026 05 15 ]
- `yyyy年MM月dd日` [ 2026年05月15日 ]
- `dd/MMM/yyyy` [ 15/May/2026 ]

### State Options (37 Indian States/UTs)
Andaman and Nicobar Islands, Andhra Pradesh, Arunachal Pradesh, Assam, Bihar, Chandigarh, Chhattisgarh, Dadra and Nagar Haveli and Daman and Diu, Daman and Diu, Delhi, Goa, Gujarat, Haryana, Himachal Pradesh, Jammu and Kashmir, Jharkhand, Karnataka, Kerala, Ladakh, Lakshadweep, Madhya Pradesh, Maharashtra, Manipur, Meghalaya, Mizoram, Nagaland, Odisha, Puducherry, Punjab, Rajasthan, Sikkim, Tamil Nadu, Telangana, Tripura, Uttar Pradesh, Uttarakhand, West Bengal

## Buttons & Actions

| Button / Link | Label | State | Action |
|---------------|-------|-------|--------|
| Choose File | "Choose File" | Always enabled | Opens OS file picker for logo upload |
| Change (Filing Address) | "Change" | Always enabled | Navigates to `#/settings/orgprofile?change_filing_address=true` — opens a modal/flow to change the registered filing address |
| Change Setting (email) | "Change Setting" | Always enabled (inline link) | Inline link `href="#"` — likely opens a modal to allow using public-domain email as sender |
| Configure Sender Email Preferences | "Configure Sender Email Preferences" | Always enabled | Navigates to `#/settings/email-preference` |
| Save | "Save" | Always enabled | Submits the form; saves all editable fields to the organisation profile |

## Tabs (if any)
No tabs. Single-page form.

## Conditional Logic

1. **Email sender warning banner**: Displayed when the primary contact email is on a public domain (e.g., gmail.com). Message: *"Your primary contact's email address belongs to a public domain. So, emails will be sent from message-service@mail.zohopayroll.in to prevent them from landing in the Spam folder. If you still want to send emails using the public domain, [Change Setting]."*
2. **Business Location**: Always disabled/read-only — hardcoded to "India". Zoho Payroll is India-only.
3. **Filing Address**: Read-only on the main form; editing requires clicking the "Change" link which takes user to a separate sub-page/modal (`?change_filing_address=true`).

## Cross-Module Impact

| Setting | Impacts |
|---------|---------|
| Organisation Name | Printed on payslips, Form 16, TDS worksheets, all statutory filings |
| Organisation Logo | Displayed on payslips, TDS worksheets, and other PDF documents |
| Date Format | Controls how all dates are rendered across the app UI and exported reports |
| Field Separator | Used in exported CSV/report files for date field separators |
| Organisation Address | Becomes the default address for the primary Work Location |
| Filing Address | Appears on all statutory forms (PF, ESI, PT challans, Form 16, Form 24Q) |
| Industry | May influence statutory applicability decisions (e.g., ESI eligibility by industry type) |
| Primary Contact Email | Receives system notifications, reminders, compliance alerts |
| Sender Email | Used as From/Reply-To on all outbound emails (payslips, TDS documents) |

## Observations & Notes

1. **Organisation ID is immutable** — displayed as `60071806579` in the page header. This is the platform-level tenant identifier.
2. **Business Location hardcoded to India** — no multi-country support. Confirms V1 India-only scope.
3. **Date format has a Japanese option** (`yyyy年MM月dd日`) which is incongruous for an Indian payroll product — likely a shared UI component from Zoho Books/CRM.
4. **Field Separator** applies to date separators in exports, not CSV column separators — current value `/` aligns with Indian date convention.
5. **Filing Address vs Organisation Address**: Two distinct concepts. Org address = primary work location address. Filing address = registered address used on statutory documents. These can differ (e.g., registered office vs. operational HQ).
6. **Change Filing Address** uses a query parameter pattern (`?change_filing_address=true`) rather than a separate route — likely a modal overlay.
7. **Public domain email warning** is a deliverability safeguard — enforces use of Zoho's sending domain when user has a gmail.com/yahoo.com primary contact. This is a sensible default.
8. **No PAN or TAN fields** on this page — those are on the Tax Details settings page (`#/settings/taxes`).
9. Industry dropdown includes "Internal" as an option — likely a Zoho-internal category not intended for public use.

## Screenshots
`docs/ba-audit/settings/screenshots/01-org-profile.png`
