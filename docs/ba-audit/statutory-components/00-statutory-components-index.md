# Statutory Components Settings — Audit Index

**Module:** Settings > Setup & Configurations > Statutory Components  
**URL:** `https://payroll.zoho.in/#/settings/statutory-details/list`  
**Audit Date:** 2026-05-15  
**Session:** Continuation of Zoho Payroll Reference Audit

---

## Navigation

5 tabs in sub-navigation:

| Tab | URL | Status in lerno org |
|-----|-----|---------------------|
| EPF | `#/settings/statutory-details/list` | Not configured (onboarding state) |
| ESI | `#/settings/statutory-details/list/esi` | Not configured (onboarding state) |
| Professional Tax | `#/settings/statutory-details/list/pt` | Configured (Kerala, Half Yearly) |
| Labour Welfare Fund | `#/settings/statutory-details/list/lwf` | Pre-populated (Kerala ₹50/₹50 Monthly, Disabled) |
| Statutory Bonus | `#/settings/statutory-details/list/statutory-bonus` | Not configured (onboarding state) |

---

## Items Documented

| Item | File | Description |
|------|------|-------------|
| 95 | [95-epf-settings.md](95-epf-settings.md) | EPF configuration form — full field audit |
| 96 | [96-esi-settings.md](96-esi-settings.md) | ESI configuration form — full field audit |
| 97 | [97-pt-settings.md](97-pt-settings.md) | PT settings — Kerala slabs + Revise slab form |
| 98 | [98-lwf-settings.md](98-lwf-settings.md) | LWF settings — Kerala pre-populated, Enable flow |
| 99 | [99-statutory-bonus-settings.md](99-statutory-bonus-settings.md) | Statutory Bonus — Monthly vs Yearly, Min Wage table |

---

## Key Findings Summary

1. **EPF not enabled** in lerno org — "Enable EPF" CTA on landing. Configuration form has 10+ fields including contribution rates, 6 checkboxes, LOP configuration.
2. **ESI rates are hardcoded** — Employee 0.75%, Employer 3.25% displayed as read-only. Only configurable field is the ESI Number.
3. **PT is work-location scoped**, not org-wide — each location (Head Office, branches) has its own PT Number, State, Deduction Cycle, and slabs. Slab revision has "Effective From" date for future-dated changes.
4. **LWF is state-pre-populated but disabled** — Kerala ₹50/₹50 amounts are system-supplied (not user-editable), only Enable/Disable toggle is user-controlled.
5. **Statutory Bonus has a Payment Bonus Act compliance note** — rate must be 8.33% (min) to 20% (max). Monthly vs Yearly modes differ in fields. Yearly mode requires Payout Month + minimum wage per employment category per state.
6. **EPF employer contribution splits into EPS + EPF** — EPS = 8.33% capped at ₹15,000 wage; EPF = remainder. Sample calculation panel shown inline.
7. **"Preview EPF Calculation" tool** — interactive calculator for testing configurations before enabling.
8. **PT Deduction Cycle is location-specific** — Kerala defaults to Half Yearly. Cannot be changed on the Revise slabs form (read-only).

---

## Screenshots

- `screenshots/95-epf-not-configured.png` — EPF onboarding state
- `screenshots/95-epf-config-form.png` — EPF configuration form (full)
- `screenshots/95-epf-employer-splitup.png` — Employer contribution splitup panel
- `screenshots/96-esi-config-form.png` — ESI configuration form
- `screenshots/97-pt-kerala-slabs.png` — PT Kerala slab view modal
- `screenshots/97-pt-revise-slabs-form.png` — PT Revise Slabs form
- `screenshots/98-lwf-settings.png` — LWF settings page
- `screenshots/99-statutory-bonus-config.png` — Statutory Bonus configuration form
