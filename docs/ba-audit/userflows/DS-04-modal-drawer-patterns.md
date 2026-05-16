# DS-04: Modal and Drawer Patterns

**Module:** Design System — UI Patterns
**Tested:** 2026-05-16
**Observed across:** Pay Runs, Employees, Loans, Approvals, Settings

---

## Pattern Types Observed

Zoho Payroll uses two primary overlay patterns:
1. **Modal Dialog** — Center-screen overlay for focused actions
2. **Side Drawer** (Entity Detail Panel) — Right-side slide-in for entity details

---

## 1. Modal Dialogs

### Characteristics
- Appears centered in viewport
- Dark semi-transparent backdrop (overlay)
- Closes on: Escape key, Cancel button, backdrop click (varies)
- Width: Typically 480–640px
- Prevents interaction with background content

### Modal Types Observed

#### Confirmation Modal
Used for: Irreversible actions (delete, finalize, cancel)

**Structure:**
```
[Icon — warning triangle or question mark]
[Title — "Are you sure?"]
[Body — consequence description]
[Secondary Button — "Cancel"]    [Primary Button — "Confirm" / "Delete"]
```

**Examples observed:**
- Delete loan confirmation
- Finalize pay run confirmation
- Delete recorded payment confirmation

#### Form Modal
Used for: Create/Edit operations that are short enough to not need a full page

**Structure:**
```
[X close button — top right]
[Title — "Create Loan" / "Record Repayment"]
[Form body — fields]
[Cancel button]    [Save/Submit button]
```

**Examples observed:**
- Create Loan modal (multi-field form with scrolling)
- Record Repayment modal
- Add Bank Account modal
- Create Off-Cycle Pay Run modal

#### Info / Alert Modal
Used for: Information display or required acknowledgment

**Structure:**
```
[Title — "Meet the New Reports Centre"]
[Content — description / image]
[Close button or "Got it" CTA]
```

**Examples observed:**
- "Meet the New Reports Centre" welcome dialog (UF-77)

---

## 2. Side Drawer (Entity Detail Panel)

### Characteristics
- Slides in from the right side of the screen
- Width: Typically 400–500px (varies by content)
- Background page remains visible but partially dimmed
- Header: Entity name, status badge, action buttons
- Tabs: For multi-section content
- Closes on: X button, Escape, clicking outside (varies)

### Side Drawer Structure
```
┌──────────────────────────────────┐
│ [X]  [Entity Name]   [Status]    │
│      [Primary Action Button]     │
│      [Three-dot menu]            │
├──────────────────────────────────┤
│ [Tab 1] [Tab 2] [Tab 3]          │
├──────────────────────────────────┤
│                                  │
│  [Tab Content]                   │
│  Fields, tables, summaries       │
│                                  │
│                                  │
└──────────────────────────────────┘
```

### Side Drawers Observed

#### Loan Detail Panel (from UF-63)
- Header: Loan ID, "Open" status badge, "Record Repayment" button, three-dot menu
- Tabs: Details, Repayment Schedule
- Details tab: Loan amount, instalment, dates, reason, perquisite
- Repayment Schedule: Empty table with "No instalments yet" message

#### Employee Detail Panel (likely — not directly confirmed)
- Header: Employee name, Employee ID, status badge
- Quick actions: Edit, more options
- Shows key fields: Position, Department, DOJ, Salary

---

## 3. Toast Notifications

### Types
| Type | Color | Icon | Use Case |
|------|-------|------|----------|
| Success | Green | Checkmark | Save successful, payment recorded |
| Error | Red | X | Save failed, validation error |
| Warning | Yellow/Orange | Warning triangle | Data missing, configuration incomplete |
| Info | Blue | i | System message, tip |

### Characteristics
- Appears: Top-right or bottom-right corner
- Auto-dismisses: After 3–5 seconds
- Can dismiss manually: X button
- Stacks: Multiple toasts stack vertically

---

## 4. Inline Form Validation

### Error Display Pattern
- Field turns red border
- Error message appears below field in red text
- Error icon inside field (optional)

**Example:**
```
[PAN Number          ] ← red border
ABCDE1234F
✕ PAN must be in format XXXXX0000X   ← red error text
```

### Validation Timing
- On blur (when user leaves field): Most fields
- On submit: Remaining uncaught errors
- Real-time: Some format-based fields (PAN, IFSC)

---

## 5. Loading States

### Table Loading
- Skeleton rows (grey animated placeholder rows)
- Spinner in table center for short loads

### Button Loading
- Button text changes to "Saving..." or "Processing..."
- Button becomes disabled (no double-submit)
- Spinner icon replaces or supplements button text

### Page Loading
- Full-page spinner for initial navigation
- Partial spinner for section loads (tabs)

---

## 6. Empty States

From DS-03 and direct observation:

| Context | Message |
|---------|---------|
| No pay runs | "No pay runs yet. Create your first pay run." |
| No loans | "No loans. Add a loan to get started." |
| Repayment schedule (no EMIs) | "The employee is yet to pay the first instalment through Zoho Payroll." |
| No reimbursement claims | "No claims submitted yet." |
| No approvals | "No items pending approval." |
| TDS Liabilities - no data | "No liabilities for this period." |

**Empty state pattern:**
```
[Illustration / icon]
[Primary message — what's empty]
[Secondary message — why or hint]
[CTA button — optional: "Add First Item"]
```

---

## 7. Confirmation Dialogs for Destructive Actions

All destructive/irreversible actions require a two-step confirmation:

**Step 1:** User clicks action button (e.g., "Delete Loan")
**Step 2:** Confirmation modal appears with:
- Warning icon
- Description of consequence
- "Cancel" (safe default)
- "Confirm" / "Delete" (styled as danger — red button)

---

## Business Rules / Design Patterns
1. Destructive actions always require two-step confirmation
2. Form modals have explicit Cancel + Save buttons
3. Side drawers used for entity detail inspection; modals for action forms
4. Toast notifications confirm async operations (save, delete, process)
5. Loading states on all async operations (no blank screens)
6. Empty states always include a CTA or explanation

## Open Questions
- [ ] Are there keyboard shortcuts to close modals (Escape) — confirmed for most?
- [ ] Do form modals have auto-save / draft-save behavior?
- [ ] Are there any multi-step wizard modals (more than 2 steps)?
- [ ] Is there a "Discard changes?" confirmation when closing a form modal with unsaved changes?
