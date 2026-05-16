# Giving Module — Audit Index

**Audit Date:** 2026-05-15
**Module URL:** `#/donations`
**Actual Route Used:** Ember router `transitionTo('donations')` (direct URL `#/donations` redirects to `#/loans` on page load — see Item 93 for full explanation)
**API Base:** `GET /api/v1/donations`, `GET /api/v1/donations/editpage`

## Files in this Directory

| File | Coverage |
|------|----------|
| `93-giving-overview.md` | Module navigation, URL behavior, first-load state, campaign list views |
| `94-giving-features.md` | Campaign creation form, fields, exemption types, employee portal integration, reports |

## Module Summary

The Giving module allows organizations to run donation campaigns where employees contribute voluntarily through payroll deductions. Contributions are tracked against the 80G income tax exemption framework (labeled "Section 133" internally). The module is fully functional — not premium-locked — and integrates with payroll tax calculations and Form 16.

## Key Findings

1. URL `#/donations` has a routing conflict: navigating directly redirects to Loans in some Ember lifecycle states. Use Ember's `transitionTo('donations')` from within the app.
2. Exemption types confirm 80G integration: 100%, 50%, or None.
3. No NGO partnerships listed — admin creates campaigns with their own charity details.
4. No employee-facing campaign opt-out UI visible in admin view — must be in Employee Portal.
5. Two donation-related reports exist: `employee-donation-summary` and `employee-donation-details`.
