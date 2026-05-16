# DS-05: Toast and Notification Patterns

**Module:** Design System — Notifications
**Tested:** 2026-05-16
**Observed across:** All modules during save operations, pay run processing, settings changes

---

## In-App Toast Notifications

### Positioning
- **Appears:** Top-right corner of viewport (Zoho standard pattern)
- **Z-index:** Above all page content and modals
- **Max visible:** 3–5 stacked toasts at a time (older ones dismiss)

### Toast Structure
```
┌─────────────────────────────────┐
│ [Icon] [Title]                  │ ← [X dismiss]
│        [Optional description]   │
└─────────────────────────────────┘
```

### Toast Types

| Type | Icon | Color | Auto-Dismiss |
|------|------|-------|-------------|
| Success | ✓ Checkmark | Green (#22C55E) | 3–4 seconds |
| Error | ✗ X mark | Red (#EF4444) | Manual (stays until dismissed) |
| Warning | ⚠ Triangle | Orange (#F97316) | 5 seconds |
| Info | ℹ Circle | Blue (#3B82F6) | 4 seconds |

### Error Toast Behavior
- Errors stay visible until manually dismissed (X button)
- Critical errors may show "Contact Support" link
- Validation errors typically shown inline (not toast) — toast for server errors

---

## Observed Toast Messages

### Successful Operations
| Action | Toast Message |
|--------|--------------|
| Employee saved | "Employee details saved successfully." |
| Loan created | "Loan created successfully." |
| Pay run finalized | "Pay run finalized." or "Pay run marked as paid." |
| Payslip sent | "Payslip sent to [employee email]." |
| Settings saved | "Settings updated successfully." |
| Salary structure saved | "Salary structure saved." |

### Error Toasts
| Action | Toast Message |
|--------|--------------|
| Form 16 prerequisite missing | "Tax Deductor not found. Please configure TAN details." |
| Form 24Q prerequisite | "Configure Tax Deductor details before generating Form 24Q." |
| API error | "Something went wrong. Please try again." |
| Network error | "Unable to connect. Check your internet connection." |

---

## Email Notifications

Zoho Payroll sends email notifications for specific events:

### Admin → Employee Emails
| Event | Trigger | Email Content |
|-------|---------|--------------|
| Payslip ready | Pay run marked as paid | "Your payslip for [Month] is ready." |
| Offer Letter | Document uploaded | "Your offer letter is available." |
| IT Declaration reminder | Admin triggers | "Please submit your tax declarations by [date]." |
| POI submission reminder | Admin triggers | "Please submit proof of investments." |

### Employee Portal Contact
From UF-84 (Employee Portal Settings):
- **Portal Contact Email:** abhijithss2255@gmail.com
- Employees can contact this email for payroll queries
- Shown in portal help/contact section

---

## System Alerts / Banner Messages

### Dashboard Onboarding Banner
From UF-84:
- Progress indicator: 5/7 steps complete
- Steps listed with checkmarks / incomplete indicators
- Sticky at top of dashboard until all steps complete

### Compliance Alerts (Expected)
| Alert Type | Trigger | Location |
|-----------|---------|----------|
| PT Number missing | PT configured but no number | Settings banner |
| TAN not configured | Form 16/24Q blocked | Taxes & Forms page |
| IT Declaration locked | All TDS = ₹0 | Dashboard or TDS section |
| EPF Number mismatch | Flagged manually (not auto-detected) | — |
| Pay run pending | Month-end reminder | Dashboard |

---

## In-App Notification Center (Expected)

Zoho products typically have a bell icon (🔔) in the top navigation for in-app notifications.

### Expected In-App Notifications
| Notification | Trigger |
|-------------|---------|
| "Pay run for June is ready to process" | Month start |
| "Reimbursement claim submitted by [Employee]" | Employee submits |
| "IT Declaration submitted by [Employee]" | Employee submits |
| "POI submitted by [Employee]" | Employee submits |
| "Loan approval required" | Loan created |
| "TDS due by 7th [Month]" | Monthly reminder |
| "Form 24Q due by 31st [Month]" | Quarterly reminder |

---

## Email vs In-App — Notification Channels

| Channel | Urgency | Use Cases |
|---------|---------|-----------|
| Toast | Immediate (during session) | Save confirmations, errors |
| Email | Async | Payslips, declarations, reminders |
| In-app bell | Async | Approvals, compliance alerts |
| Dashboard banner | Persistent | Setup incomplete, compliance warnings |

---

## Employee Portal Notifications

From UF-84:
- **Banner Message:** Configurable by admin; shown on employee portal home
- **Portal Contact:** Email shown to employees for queries

Employee portal (mobile app) likely has push notifications:
| Push Notification | Trigger |
|-----------------|---------|
| "Your salary for May is credited" | Pay run finalized |
| "Reimbursement claim approved" | Admin approves |
| "IT Declaration open. Submit now." | Admin releases declaration |
| "Form 16 available" | Admin publishes |

---

## Business Rules
1. Error toasts stay until manually dismissed — user must acknowledge errors
2. Success toasts auto-dismiss after ~4 seconds
3. Payslip emails sent only when admin explicitly triggers "Send Payslip" (not automatic)
4. Admin controls payslip email timing — not automatic on finalization
5. Employee portal contact email is a single org-wide contact (not per-employee)
6. Auto-reminder for IT Declaration is in Settings > Preferences (schedule configurable)

## Gaps / Observations
- In-app notification bell not directly observed / explored
- Push notifications on mobile app not tested
- Email notification templates not reviewed (HTML/text content not captured)
- Reminder schedule settings not navigated

## Open Questions
- [ ] Is there a notification settings page where admin can configure which events trigger emails?
- [ ] Can employees opt out of email notifications?
- [ ] Does Zoho provide WhatsApp notification option (popular for Indian businesses)?
- [ ] Is there a notification log showing history of all emails sent?
