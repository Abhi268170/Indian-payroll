# DS-06: Color System and Design Tokens

**Module:** Design System — Visual Language
**Tested:** 2026-05-16
**Observed across:** All modules; inferred from Zoho's design system conventions

---

## Zoho Design System (Catalyst)

Zoho Payroll is built on **Zoho Catalyst** — Zoho's internal design system. It provides consistent component styles, spacing, typography, and color tokens across all Zoho products.

---

## Color Palette

### Brand Colors
| Token | Hex | Usage |
|-------|-----|-------|
| Primary Blue | #0C6AC9 (approx) | Primary buttons, links, active states |
| Primary Light | #E8F2FC | Selected row highlights, active tab background |
| Zoho Blue | #1565C0 | Zoho brand color; top navigation |

### Semantic Colors

#### Status Colors
| Status | Color | Hex (approx) | Usage |
|--------|-------|-------------|-------|
| Success / Active / Paid | Green | #22C55E / #16A34A | PAID badge, Active employee, Success toast |
| Warning / Pending | Orange / Amber | #F97316 / #D97706 | Pending items, Draft state, Warning toast |
| Error / Critical | Red | #EF4444 / #DC2626 | Error toast, 🔴 gaps, Rejected status |
| Info | Blue | #3B82F6 / #2563EB | Info toast, informational badges |
| Neutral / Inactive | Grey | #6B7280 / #9CA3AF | Inactive status, disabled states |
| Skipped | Grey-Blue | #94A3B8 | Skipped pay run employees |

### Background Colors
| Token | Hex (approx) | Usage |
|-------|-------------|-------|
| Page Background | #F8F9FA / #F3F4F6 | Main content area |
| Card Background | #FFFFFF | Cards, panels, tables |
| Sidebar Background | #1E293B / #0F172A | Left navigation sidebar (dark) |
| Top Band | #FFFFFF / Zoho Blue | Top navigation bar |
| Table Row Hover | #F1F5F9 | Table row on hover |
| Selected Row | #EBF5FB | Table row selected (checkbox) |

---

## Typography Scale

### Font Family
| Usage | Font |
|-------|------|
| Primary | "Lato" or system font stack |
| Numbers / Currency | Monospace or Lato |
| Code / Technical | Courier / monospace |

### Type Scale
| Level | Size | Weight | Usage |
|-------|------|--------|-------|
| Page Title | 24px | 600 (Semi-bold) | Page headings |
| Section Header | 18px | 600 | Card titles, section headers |
| Body | 14px | 400 | Form labels, table content |
| Caption | 12px | 400 | Help text, tooltips, secondary info |
| Badge | 11–12px | 600 | Status badges |

---

## Status Badge System

Observed across employees, pay runs, loans, approvals:

| Status | Badge Style | Color |
|--------|-------------|-------|
| PAID | Green filled pill | #22C55E background, white text |
| ACTIVE | Green filled pill | #22C55E |
| OPEN (Loan) | Blue outline pill | #3B82F6 border, blue text |
| PENDING | Orange filled pill | #F97316 |
| DRAFT | Grey outline pill | #9CA3AF |
| SKIPPED | Grey filled pill | #6B7280 |
| INACTIVE | Grey outline | #9CA3AF |
| REJECTED | Red filled pill | #EF4444 |
| CLOSED (Loan) | Grey filled | #6B7280 |

---

## Icon System

Zoho Payroll uses a custom icon library (Catalyst Icons or Remix Icons):

| Icon | Usage |
|------|-------|
| Pencil / Edit | Edit action |
| Trash / Bin | Delete action |
| Download arrow | Download |
| Upload arrow | Upload |
| Three-dot (⋮) | More actions dropdown |
| Chevron right (›) | Navigate / expand |
| Checkmark (✓) | Completed, success |
| X mark | Close, error, clear |
| Warning triangle | Warning, caution |
| Info circle | Information, help |
| User silhouette | Employee, user |
| Rupee (₹) | Currency fields |
| Calendar | Date fields |
| Lock | Locked / immutable state |
| Bell | Notifications |

---

## Spacing System

Standard spacing values (Zoho Catalyst 4px base grid):

| Token | Size | Usage |
|-------|------|-------|
| xs | 4px | Tight spacing, icon gaps |
| sm | 8px | Component internal padding |
| md | 16px | Form field spacing |
| lg | 24px | Section spacing |
| xl | 32px | Page section gaps |
| 2xl | 48px | Major section dividers |

---

## Component Sizing

### Buttons
| Variant | Height | Padding | Usage |
|---------|--------|---------|-------|
| Large | 40px | 16px H | Primary page actions |
| Medium | 36px | 12px H | Secondary actions, table row actions |
| Small | 28px | 8px H | Inline actions, badges with action |
| Icon-only | 32×32px | — | Toolbar icons |

### Input Fields
| Size | Height | Usage |
|------|--------|-------|
| Standard | 36px | Form fields |
| Compact | 28px | Table inline edit |

---

## Indian-Specific Display Conventions

| Convention | Format | Example |
|-----------|--------|---------|
| Currency | ₹X,XX,XXX.XX (lakh system) | ₹1,25,000.00 |
| Large numbers | Indian comma grouping | ₹12,50,000 not ₹1,250,000 |
| Date format | DD/MM/YYYY | 01/04/2026 |
| Financial year | FY2026-27 | April 2026 – March 2027 |
| PAN display | Masked partially (sometimes) | ABCDE1234F |
| Aadhaar | Always masked | XXXX-XXXX-1234 |
| Mobile numbers | +91-XXXXXXXXXX | +91-9876543210 |

---

## Dark Mode

Not observed in Zoho Payroll — the application uses light mode only. No dark mode toggle observed.

---

## Responsive Design

Zoho Payroll web:
- Desktop-first design (1280px+ optimal)
- Not fully responsive for mobile browsers (mobile use via dedicated app)
- Minimum viable viewport: ~1024px width

Employee Portal:
- Mobile-first (iOS + Android apps)
- Responsive web version may be limited

---

## Business Rules (Design)
1. Indian number formatting (lakh system) used throughout — no Western million grouping
2. All currency shown with ₹ prefix and 2 decimal places
3. Dates in DD/MM/YYYY — no ambiguous MM/DD/YYYY format
4. Aadhaar always masked in all views
5. Status badges use consistent color semantics across all modules
6. Destructive actions (delete, finalize, cancel) use red buttons or require confirmation

## Open Questions
- [ ] Is there a configurable date format (for orgs with international employees who expect MM/DD/YYYY)?
- [ ] Does the app support right-to-left text (for orgs with Arabic/Hebrew employees)?
- [ ] Are there accessibility features: screen reader support, WCAG compliance level?
- [ ] Does Zoho Payroll have a white-label / custom branding option for enterprise clients?
