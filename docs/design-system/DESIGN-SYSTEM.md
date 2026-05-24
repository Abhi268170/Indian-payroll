# Indian Payroll — Design System

Extracted from Zoho Payroll India reference audit (2026-05-16, 117 audit files).
This is the single source of truth for all UI work in this project.

---

## Stack

- **React 19** + **TypeScript** (strict)
- **Tailwind CSS v4** — utility-first, CSS custom properties in `index.css`
- **No component library** (no MUI, no shadcn, no Ant Design) — build from these specs
- All class composition via `clsx` + `tailwind-merge`

---

## 1. Color Tokens

Defined as CSS custom properties in `web/src/index.css`. Use via Tailwind arbitrary values or
direct CSS vars.

```css
/* Primitives */
--color-primary: #0c6ac9;          /* primary blue — buttons, links, active states */
--color-primary-light: #e8f2fc;    /* selected row, active tab bg */
--color-primary-hover: #0a5aaa;    /* button hover */

--color-sidebar-bg: #1e293b;       /* dark sidebar */
--color-sidebar-hover: #334155;    /* sidebar item hover */
--color-sidebar-active: #2563eb;   /* sidebar active item bg */
--color-sidebar-text: #cbd5e1;     /* sidebar item text */
--color-sidebar-text-active: #ffffff;

--color-topbar-bg: #ffffff;
--color-page-bg: #f8fafc;          /* main content area background */
--color-card-bg: #ffffff;
--color-border: #e2e8f0;
--color-border-strong: #cbd5e1;

/* Semantic */
--color-success: #16a34a;
--color-success-bg: #dcfce7;
--color-warning: #d97706;
--color-warning-bg: #fef3c7;
--color-error: #dc2626;
--color-error-bg: #fee2e2;
--color-info: #2563eb;
--color-info-bg: #dbeafe;

/* Text */
--color-text-primary: #0f172a;
--color-text-secondary: #475569;
--color-text-muted: #94a3b8;
--color-text-disabled: #cbd5e1;
--color-text-inverse: #ffffff;

/* Status badge backgrounds (light fills) */
--color-badge-green-bg: #dcfce7;
--color-badge-green-text: #15803d;
--color-badge-blue-bg: #dbeafe;
--color-badge-blue-text: #1d4ed8;
--color-badge-orange-bg: #fed7aa;
--color-badge-orange-text: #c2410c;
--color-badge-red-bg: #fee2e2;
--color-badge-red-text: #b91c1c;
--color-badge-grey-bg: #f1f5f9;
--color-badge-grey-text: #475569;
```

---

## 2. Typography

Font: **Inter** (Google Fonts) — fallback: `system-ui, -apple-system, sans-serif`.
Add `<link>` for Inter in `index.html`.

| Role | Size | Weight | Class |
|------|------|--------|-------|
| Page title | 20px | 600 | `text-[20px] font-semibold` |
| Section header | 15px | 600 | `text-[15px] font-semibold` |
| Card title | 14px | 600 | `text-sm font-semibold` |
| Body / labels | 14px | 400 | `text-sm` |
| Table content | 13px | 400 | `text-[13px]` |
| Caption / help text | 12px | 400 | `text-xs` |
| Badge text | 11px | 500 | `text-[11px] font-medium` |

All text: `antialiased`. Currency: monospace variant via `font-variant-numeric: tabular-nums`.

---

## 3. Spacing

4px base grid. Use Tailwind scale: `1=4px, 2=8px, 3=12px, 4=16px, 5=20px, 6=24px`.

---

## 4. Application Shell

```
┌─────────────────────────────────────────────────────────┐
│  TOP BAR  (h-14, white, border-bottom, sticky)          │
├──────────┬──────────────────────────────────────────────┤
│          │                                              │
│ SIDEBAR  │  MAIN CONTENT AREA                          │
│ w-56     │  bg-page, overflow-y-auto                   │
│ dark bg  │  px-6 py-5                                  │
│ sticky   │                                              │
│          │  [Page header: title + actions]             │
│          │  [Tab bar (optional)]                       │
│          │  [Content: table / form / cards]            │
│          │                                             │
│          │  [Side drawer (slides from right)]         │
└──────────┴──────────────────────────────────────────────┘
```

### Top Bar (AppShell header)
- Height: `h-14`
- Background: `bg-white border-b border-[--color-border]`
- Left: logo + product name ("Payroll")
- Right: org switcher, notifications icon, settings icon, user avatar
- Position: `sticky top-0 z-40`

### Sidebar
- Width: `w-56` (collapsible to `w-14` icon-only mode)
- Background: `bg-[--color-sidebar-bg]`
- Logo area: `h-14` to align with top bar
- Nav items: `h-9 px-3 rounded-lg text-[13px]`
- Active: `bg-[--color-sidebar-active] text-white`
- Hover: `hover:bg-[--color-sidebar-hover] text-[--color-sidebar-text]`
- Section groups: collapsible, chevron rotates on open
- Bottom: user info + sign out

### Main Content
- Background: `bg-[--color-page-bg]`
- Padding: `px-6 py-5`
- Max content width: none (full width)

---

## 5. Page Header Pattern

Every list/detail page uses this header structure:

```tsx
<div className="flex items-center justify-between mb-5">
  <div>
    <h1 className="text-[20px] font-semibold text-[--color-text-primary]">Page Title</h1>
    <p className="text-xs text-[--color-text-muted] mt-0.5">Optional subtitle</p>
  </div>
  <div className="flex items-center gap-2">
    {/* Action buttons */}
  </div>
</div>
```

---

## 6. Buttons

### Variants

```tsx
// Primary — filled blue
<button className="inline-flex items-center gap-1.5 h-9 px-4 bg-[--color-primary]
  hover:bg-[--color-primary-hover] text-white text-sm font-medium rounded-lg
  disabled:opacity-50 disabled:cursor-not-allowed transition-colors">

// Secondary — outlined
<button className="inline-flex items-center gap-1.5 h-9 px-4 border border-[--color-border-strong]
  bg-white hover:bg-gray-50 text-[--color-text-primary] text-sm font-medium rounded-lg
  disabled:opacity-50 transition-colors">

// Ghost — text only
<button className="inline-flex items-center gap-1.5 h-9 px-3 text-[--color-primary]
  hover:bg-[--color-primary-light] text-sm font-medium rounded-lg transition-colors">

// Danger — destructive action
<button className="inline-flex items-center gap-1.5 h-9 px-4 bg-[--color-error]
  hover:bg-red-700 text-white text-sm font-medium rounded-lg
  disabled:opacity-50 transition-colors">

// Icon-only
<button className="inline-flex items-center justify-center w-9 h-9 rounded-lg
  text-[--color-text-secondary] hover:bg-gray-100 transition-colors">
```

### Loading state
- Replace text with spinner + "Saving…"
- `disabled` while loading

---

## 7. Form Components

### Text Input

```tsx
<div className="space-y-1">
  <label className="block text-sm font-medium text-[--color-text-primary]">
    Label <span className="text-[--color-error]">*</span>
  </label>
  <input
    className="w-full h-9 px-3 border border-[--color-border] rounded-lg text-sm
      bg-white text-[--color-text-primary] placeholder:text-[--color-text-muted]
      focus:outline-none focus:ring-2 focus:ring-[--color-primary] focus:border-[--color-primary]
      disabled:bg-gray-50 disabled:text-[--color-text-disabled]
      aria-[invalid=true]:border-[--color-error] aria-[invalid=true]:ring-[--color-error]"
  />
  {/* Error */}
  <p className="text-xs text-[--color-error]">Error message</p>
  {/* Help text */}
  <p className="text-xs text-[--color-text-muted]">Helper text</p>
</div>
```

### Select / Dropdown

```tsx
<select className="w-full h-9 px-3 border border-[--color-border] rounded-lg text-sm
  bg-white text-[--color-text-primary]
  focus:outline-none focus:ring-2 focus:ring-[--color-primary]
  disabled:bg-gray-50">
```

### Currency Input
- Wrap in a relative container
- Prefix `₹` as absolute-positioned span left-3
- Input gets `pl-7`

### Toggle Switch

```tsx
<label className="relative inline-flex items-center cursor-pointer gap-2">
  <input type="checkbox" className="sr-only peer" />
  <div className="w-9 h-5 bg-gray-200 rounded-full peer
    peer-checked:bg-[--color-primary]
    after:content-[''] after:absolute after:top-0.5 after:left-0.5
    after:w-4 after:h-4 after:bg-white after:rounded-full after:transition-transform
    peer-checked:after:translate-x-4" />
  <span className="text-sm text-[--color-text-primary]">Label</span>
</label>
```

### Validation pattern
- `aria-invalid="true"` on input when error
- Error `<p>` below field: `text-xs text-[--color-error]`
- Help text: `text-xs text-[--color-text-muted]`
- Validate on blur; show all on submit attempt

---

## 8. Tables

```tsx
<div className="border border-[--color-border] rounded-xl overflow-hidden">
  <table className="w-full text-[13px]">
    <thead>
      <tr className="bg-gray-50 border-b border-[--color-border]">
        <th className="text-left px-4 py-3 font-medium text-[--color-text-secondary] whitespace-nowrap">
          Column
        </th>
      </tr>
    </thead>
    <tbody className="divide-y divide-[--color-border]">
      <tr className="hover:bg-[--color-primary-light] transition-colors">
        <td className="px-4 py-3 text-[--color-text-primary]">Value</td>
      </tr>
    </tbody>
  </table>
</div>
```

### Table toolbar (above table)
```tsx
<div className="flex items-center justify-between mb-3">
  <div className="flex items-center gap-2">
    {/* Search input, filters */}
  </div>
  <div className="flex items-center gap-2">
    {/* Export, bulk actions */}
  </div>
</div>
```

### Empty state (inside table)
```tsx
<tr>
  <td colSpan={N} className="px-4 py-16 text-center">
    <div className="flex flex-col items-center gap-2">
      <div className="w-10 h-10 rounded-full bg-gray-100 flex items-center justify-center">
        {/* Icon */}
      </div>
      <p className="text-sm font-medium text-[--color-text-primary]">No items yet</p>
      <p className="text-xs text-[--color-text-muted]">Create your first item to get started.</p>
      <button className="mt-1 ...primary button...">Add Item</button>
    </div>
  </td>
</tr>
```

---

## 9. Cards

```tsx
<div className="bg-white rounded-xl border border-[--color-border] p-5">
  {/* Card header */}
  <div className="flex items-center justify-between mb-4">
    <h3 className="text-sm font-semibold text-[--color-text-primary]">Section Title</h3>
    <button>Edit</button>
  </div>
  {/* Content */}
</div>
```

---

## 10. Status Badges

```tsx
// Green — Active, Paid, Approved
<span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-medium
  bg-[--color-badge-green-bg] text-[--color-badge-green-text]">Active</span>

// Blue — Draft, In Progress
<span className="... bg-[--color-badge-blue-bg] text-[--color-badge-blue-text]">Draft</span>

// Orange — Pending, Locked
<span className="... bg-[--color-badge-orange-bg] text-[--color-badge-orange-text]">Pending</span>

// Red — Rejected, Inactive
<span className="... bg-[--color-badge-red-bg] text-[--color-badge-red-text]">Rejected</span>

// Grey — Skipped, Closed
<span className="... bg-[--color-badge-grey-bg] text-[--color-badge-grey-text]">Skipped</span>
```

---

## 11. Side Drawer

Right-side slide-in panel for entity detail views. Overlays the content area (not full screen).

```tsx
// Overlay
<div className="fixed inset-0 z-50" onClick={onClose} />

// Drawer panel
<aside className="fixed right-0 top-0 h-full w-[480px] bg-white border-l border-[--color-border]
  shadow-xl z-50 flex flex-col overflow-hidden
  animate-in slide-in-from-right duration-200">

  {/* Header */}
  <div className="flex items-start justify-between px-5 py-4 border-b border-[--color-border]">
    <div>
      <h2 className="text-[15px] font-semibold text-[--color-text-primary]">Entity Name</h2>
      <p className="text-xs text-[--color-text-muted] mt-0.5">ID or subtitle</p>
    </div>
    <button onClick={onClose}>✕</button>
  </div>

  {/* Tabs (optional) */}
  <div className="flex border-b border-[--color-border] px-5">
    <button className="... border-b-2 border-[--color-primary] text-[--color-primary]">Tab 1</button>
    <button className="... text-[--color-text-secondary]">Tab 2</button>
  </div>

  {/* Scrollable content */}
  <div className="flex-1 overflow-y-auto px-5 py-4">
    {/* Content */}
  </div>

  {/* Footer actions */}
  <div className="px-5 py-4 border-t border-[--color-border] flex gap-2">
    <button className="...primary...">Primary Action</button>
    <button className="...secondary...">Secondary</button>
  </div>
</aside>
```

---

## 12. Modal / Dialog

```tsx
{/* Backdrop */}
<div className="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4">
  {/* Dialog */}
  <div className="bg-white rounded-xl shadow-xl w-full max-w-md">
    {/* Header */}
    <div className="flex items-center justify-between px-5 py-4 border-b border-[--color-border]">
      <h2 className="text-[15px] font-semibold text-[--color-text-primary]">Dialog Title</h2>
      <button>✕</button>
    </div>
    {/* Body */}
    <div className="px-5 py-4">
      {/* Content */}
    </div>
    {/* Footer */}
    <div className="flex justify-end gap-2 px-5 py-4 border-t border-[--color-border]">
      <button className="...secondary...">Cancel</button>
      <button className="...primary...">Confirm</button>
    </div>
  </div>
</div>
```

**Danger confirmation modal:** Replace primary button with danger variant.

---

## 13. Toast Notifications

Use a global toast context. Toast appears **top-right**, stacks vertically.

```tsx
// Toast item
<div className="flex items-start gap-3 w-80 bg-white rounded-xl shadow-lg border
  border-[--color-border] px-4 py-3">
  <div className="mt-0.5 w-4 h-4 flex-shrink-0">
    {/* Icon: check for success, x for error, ! for warning, i for info */}
  </div>
  <div className="flex-1 min-w-0">
    <p className="text-sm font-medium text-[--color-text-primary]">{title}</p>
    {description && <p className="text-xs text-[--color-text-muted] mt-0.5">{description}</p>}
  </div>
  <button onClick={dismiss} className="text-[--color-text-muted] hover:text-[--color-text-primary]">✕</button>
</div>
```

Timing: success/info auto-dismiss 3s; warning 5s; error manual only.

---

## 14. Loading States

```tsx
// Inline spinner (button or cell)
<svg className="animate-spin w-4 h-4" ...>

// Skeleton row (table loading)
<tr className="animate-pulse">
  <td className="px-4 py-3"><div className="h-4 bg-gray-100 rounded w-32" /></td>
  ...
</tr>

// Full page loader
<div className="flex h-full items-center justify-center">
  <svg className="animate-spin w-8 h-8 text-[--color-primary]" ...>
</div>
```

---

## 15. Indian Number Formatting

ALL currency values must use Indian number system (lakh/crore grouping).

```ts
// Use this everywhere — do not use toLocaleString without explicit locale
export function formatINR(amount: number | string): string {
  const n = typeof amount === 'string' ? parseFloat(amount) : amount
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    minimumFractionDigits: 2,
  }).format(n)
}
// Result: ₹1,25,000.00 (NOT ₹125,000.00)
```

Dates: always `dd/MM/yyyy` display format. Store/send as ISO 8601.

---

## 16. Sidebar Navigation Groups

Expandable groups use chevron + collapsible sub-items:

```tsx
// Group with sub-items
<div>
  <button className="flex items-center justify-between w-full h-9 px-3 rounded-lg
    text-[--color-sidebar-text] hover:bg-[--color-sidebar-hover] text-[13px]"
    onClick={toggleOpen}>
    <span className="flex items-center gap-2">
      <Icon /> Label
    </span>
    <ChevronIcon className={clsx('w-4 h-4 transition-transform', open && 'rotate-180')} />
  </button>
  {open && (
    <div className="ml-4 mt-0.5 space-y-0.5">
      <NavLink ...sub-items... />
    </div>
  )}
</div>
```

---

## 17. Key Patterns to Replicate (from Zoho audit)

| Pattern | Implementation note |
|---------|-------------------|
| Onboarding checklist on dashboard | 7-step checklist, progress bar, "Mark Complete" per step |
| Payslip side drawer | Slide from right, earnings/deductions table, download + send actions |
| Pay run 3-tab layout | Employee Summary / Taxes & Deductions / Overall Insights |
| Employee list → side drawer detail | Click row → drawer opens right, list stays visible |
| Skip badge on pay run employee rows | Grey "SKIPPED" badge + reason text |
| Component drill-down in pay run insights | Click component → per-employee breakdown |
| Settings as overlay/modal navigation | Not a separate page — overlay with sidebar |

---

## 18. Icons

Use **Lucide React** (`lucide-react` package). Size `w-4 h-4` inline, `w-5 h-5` for sidebar.
No emoji as icons in functional UI.

```tsx
import { Users, CreditCard, Settings, ChevronDown, Plus, Download } from 'lucide-react'
```

---

## File Conventions

- UI primitives: `web/src/components/ui/` — Button, Input, Badge, Modal, Drawer, Toast, Table, etc.
- Layout shells: `web/src/components/layout/` — AppLayout, PlatformLayout
- Page components: `web/src/pages/`
- All components: named exports, explicit return types, no `any`
