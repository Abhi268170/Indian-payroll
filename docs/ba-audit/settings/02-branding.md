# Settings > Branding

## URL
`#/settings/branding`

## Purpose
Controls the visual appearance of the Zoho Payroll application (sidebar pane style and accent colour). Settings apply across all Zoho Finance apps in the organisation's Zoho account, not just Payroll.

## Page Layout
Single-section page with two sub-sections:
1. **Appearance** — Pane style selection (Dark / Light)
2. **Accent Color** — Preset colour swatches + Custom colour picker

An "Instant Helper" button (question mark icon) is available at the top-right of the content area.

## Fields

| Field | Type | Required | Default / Current | Options / Format | Help Text | Validation |
|-------|------|----------|-------------------|------------------|-----------|------------|
| Appearance (Pane style) | Toggle buttons (radio-style) | No | Dark Pane (currently selected) | Dark Pane, Light Pane | None | One must be selected |
| Accent Color | Button group (colour swatch) | No | Blue (currently selected) | Blue, Green, Red, Yellow, Custom Color | None | One must be selected |
| Custom Color (hex input) | Text input + colour picker | No (only if Custom selected) | N/A | Hex colour code; hue slider + palette picker | None | Valid hex colour |

### Appearance Options
- **Dark Pane** — Navigation sidebar uses a dark background (currently active, indicated by `selected-type` CSS class)
- **Light Pane** — Navigation sidebar uses a light/white background

### Accent Color Options
- **Blue** (CSS class: `unifiedtheme-blue`) — currently selected (`selected-theme-container`)
- **Green** (CSS class: `unifiedtheme-green`)
- **Red** (CSS class: `unifiedtheme-red`)
- **Yellow** (CSS class: `unifiedtheme-yellow`)
- **Custom Color** — opens an inline colour picker with:
  - Colour palette canvas (hue/saturation/brightness)
  - Hue slider
  - Hex input field
  - "Apply" button
  - "Cancel" button

## Buttons & Actions

| Button | Label | State | Action |
|--------|-------|-------|--------|
| Dark Pane | "Dark Pane" | Selectable | Sets navigation pane to dark mode; applies immediately |
| Light Pane | "Light Pane" | Selectable | Sets navigation pane to light mode; applies immediately |
| Blue swatch | "Blue" | Selectable | Sets accent colour to blue; applies immediately |
| Green swatch | (unlabelled in DOM) | Selectable | Sets accent colour to green |
| Red swatch | (unlabelled in DOM) | Selectable | Sets accent colour to red |
| Yellow swatch | (unlabelled in DOM) | Selectable | Sets accent colour to yellow |
| Custom Color | "Custom Color" | Selectable | Opens inline colour picker panel |
| Apply (colour picker) | "Apply" | Enabled when colour selected | Applies chosen custom hex colour |
| Cancel (colour picker) | "Cancel" | Always enabled | Dismisses colour picker without change |
| Instant Helper | icon button | Always enabled | Opens contextual help overlay for this page |

## Tabs (if any)
None. Single-section page.

## Conditional Logic

1. **Custom Color picker** — only visible/expanded when the "Custom Color" swatch button is clicked. Hidden by default.
2. **Selected state indicators** — the currently active Appearance and Accent Color options show a visual selected state (`selected-type` / `selected-theme-container` CSS classes).
3. No Save button — changes appear to apply **immediately** on selection (client-side state change broadcast across Zoho Finance apps).

## Cross-Module Impact

| Setting | Impacts |
|---------|---------|
| Appearance (Pane style) | Navigation sidebar colour scheme across Zoho Payroll, Zoho Books, Zoho Expense, and all other Zoho Finance apps in the same account |
| Accent Color | Button colours, active states, link colours, and highlights across all Zoho Finance apps |

Note: These are **account-level preferences**, not tenant/org-level. The note on the page reads: *"These preferences will be applied across Zoho Finance apps."*

## Observations & Notes

1. **No payroll-specific branding impact** — this page does not control payslip branding, email header colours, or PDF template colours. It is purely a UI theme for the web application.
2. **Shared across Zoho Finance suite** — changing here affects the user's experience in all linked Zoho Finance apps (Books, Expense, Invoice, etc.). This is a cross-product setting surfaced inside Payroll's settings for convenience.
3. **No Save button** — unlike most settings pages, changes here apply immediately on click. This is a different UX pattern from the rest of the settings, which may confuse users who expect a Save confirmation.
4. **User-level vs Org-level ambiguity** — it is unclear from the UI whether these preferences are per-user or per-organisation. Given the "Zoho Finance apps" note, these are likely per-user account preferences, not shared across all HR admins.
5. **Instant Helper button** — unique to the Branding page among settings pages reviewed so far. This is Zoho's built-in contextual help system.
6. For our own product, branding settings should include: payslip PDF header colour/logo, email template header, and potentially per-organisation theme rather than per-user.

## Screenshots
`docs/ba-audit/settings/screenshots/02-branding.png`
