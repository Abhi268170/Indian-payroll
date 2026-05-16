# Employees > Add Employee — Step 3: Personal Details (Employment Info)

## URL / Navigation Path
- Route: `#/people/employees/{id}/edit/personal-details`
- Full URL: `https://payroll.zoho.in/#/people/employees/3848927000000032948/edit/personal-details`
- Entry: Reached after saving Salary Details (Step 2) in the wizard
- Page title: "Employees | Personal Information | Employees | Edit | Zoho Payroll"

## Purpose
Step 3 of 4 in Add Employee wizard. Captures statutory identity fields (PAN, DOB, Father's Name), personal contact details, disability classification, and residential address.

## Note on Wizard Naming
Zoho's wizard steps are labelled:
1. Basic Details (name, ID, department, designation, work location, gender, joining date, email, mobile)
2. Salary Details (statutory components, salary structure, other benefits)
3. **Personal Details** (DOB, PAN, Father's Name, personal email, address)
4. Payment Information (bank account details)
5. Summary / Confirmation

The "Employment Details" information per the audit plan (department, designation, employment type, probation, reporting manager) is captured in Step 1 "Basic Details" — NOT in a separate Employment tab.

## Fields

| Field | Type | Required | Options / Format | Notes |
|---|---|---|---|---|
| Date of Birth | Text (date picker, calendar on click) | Yes | `dd/MM/yyyy` format; calendar opens on click | Age auto-calculated from DOB (displayed in read-only Age field) |
| Age | Text (read-only, disabled) | N/A | Auto-calculated integer | Calculated from DOB vs current date; not editable |
| Father's Name | Text | Yes | Free text | Required for TDS (Form 16 and quarterly returns include father's name) |
| PAN | Text | No (optional at creation) | Format: `AAAAA0000A`; 10 chars; placeholder shows format | PAN can be added later; not enforced at wizard time |
| Differently Abled Type | Custom dropdown (ac-box) | No | None / Visual / Hearing / Speech / Mobility / Other | "None" default; affects TDS (higher deduction limit for PWD employees under Section 80U) |
| Personal Email Address | Text | No | Placeholder `abc@xyz.com` | Separate from Work Email; used for personal communication |
| Residential Address — Line 1 | Text | No | Placeholder "Address Line 1" | Part of residential address block |
| Residential Address — Line 2 | Text | No | Placeholder "Address Line 2" | Optional second line |
| City | Text | No | Placeholder "City" | Part of address |
| State | Custom dropdown (ac-box) | No | 37 options: Andaman and Nicobar Islands, Andhra Pradesh, Arunachal Pradesh, Assam, Bihar, Chandigarh, Chhattisgarh, Dadra and Nagar Haveli and Daman and Diu, Daman and Diu, Delhi, Goa, Gujarat, Haryana, Himachal Pradesh, Jammu and Kashmir, Jharkhand, Karnataka, Kerala, Ladakh, Lakshadweep, Madhya Pradesh, Maharashtra, Manipur, Meghalaya, Mizoram, Nagaland, Odisha, Puducherry, Punjab, Rajasthan, Sikkim, Tamil Nadu, Telangana, Tripura, Uttar Pradesh, Uttarakhand, West Bengal | Daman and Diu listed separately AND as part of merged UT — duplication issue (noted in Session 3 as well) |
| PIN Code | Text | No | Placeholder "PIN Code" | 6-digit Indian postal code |

## Buttons & Actions

| Button | Behaviour |
|---|---|
| Save and Continue | Validates required fields (DOB, Father's Name); saves and navigates to Step 4 (Payment Information) |
| Skip | Link directly to `#/people/employees/{id}/edit/payment-details`; skips Personal Details entirely |

## Key Observations for Our Build

1. **PAN is optional at creation time** — can be added later from profile edit. Critical for our build: PAN must be collected before TDS can be computed. We should surface a warning on payroll run if PAN is missing.
2. **Father's Name is mandatory** — required for Form 16 and 24Q filing. Our Employee entity must include this field.
3. **Differently Abled Type maps to 80U/80DD** — the 6 disability types map to Income Tax Act categories. Our TDS engine needs to handle higher deduction limits for PWD employees.
4. **State in address vs Work Location state are different** — residential address state is not the PT-determining state. PT is driven by Work Location, not residence.
5. **Age is auto-calculated from DOB** — display only, not stored as a separate field.
6. **Skip is a first-class option** — Personal Details is genuinely optional at onboarding time.
7. **Daman and Diu duplication** — same compliance issue as noted in Org Profile (Session 3). Our state list must use the correct merged UT only.
8. **No Aadhaar field on this step** — despite Aadhaar being in Zoho's domain model, there is no Aadhaar capture in the wizard at all. This is a gap.
9. **No Employment Type field anywhere in wizard** — Contractor vs Permanent distinction not captured in wizard. Likely set differently (see EMP004 audit for contractor flow).
10. **No Probation Period or Notice Period** — standard HR fields not present in Zoho's basic wizard.

## EMP001 Data Filled
- DOB: 15/03/1990 → Age: 36 (auto-calculated)
- Father's Name: Rajesh Mehta
- PAN: ABCPM1234A
- Differently Abled Type: None (default)
- Personal Email: (not filled)
- Address: (not filled)

## Screenshots
- `screenshots/36-personal-details.png` — Full page of Step 3
- `screenshots/36-personal-details-filled.png` — After filling EMP001 data
