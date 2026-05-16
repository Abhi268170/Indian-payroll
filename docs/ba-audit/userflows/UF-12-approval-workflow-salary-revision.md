# UF-12: Approval Workflow — Salary Revision

**Module:** Settings > Module Settings > General > Salary Revisions
**Tested:** 2026-05-16
**Mock Data Used:** lerno org, admin user abhijithss2255
**App State Before:** Default config (no prior approval workflow saved)

## Steps Executed
1. Navigate to `#/settings/salary-revision/custom-approval/list`
2. Observed three workflow type radio buttons
3. Selected "Multi-Level Approval" — observed level configuration UI
4. Selected "Custom Approval" — observed custom workflow empty state + CTA

## Page Identity
- **URL:** `#/settings/salary-revision/custom-approval/list`
- **Page title:** "Salary Revision | Approvals | Settings | Zoho Payroll"
- **Module path in Settings:** Module Settings > General > Salary Revisions > Approvals tab
- **Access:** Admin only (Settings access required)

## Layout
- Settings sidebar on left (same global settings nav)
- Main content: heading "Salary Revisions" + tab "Approvals"
- Radio group for workflow type selection
- Dynamic content area below radio group (changes based on selection)
- Save button at bottom

## Workflow Types Available

| Type | Label | Description |
|------|-------|-------------|
| Simple Approval | Simple Approval | Any approver with Salary Revision approval permission can approve |
| Multi-Level Approval | Multi-Level Approval | Set many levels; revision approved only when ALL approvers approve |
| Custom Approval | Custom Approval | Criteria-based approval flows; multiple conditions selectable by admin |

## Multi-Level Approval Configuration

When "Multi-Level Approval" is selected, a section "Assign approvers for each level" appears:

- **Level display:** "Level 1 Approver" with a user picker combobox
- **Default value:** Current user (abhijithss2255) pre-filled
- **Delete button:** Per-level delete icon
- **"Add New Level" button:** Adds another level row (no visible maximum level count stated)
- **Approver picker type:** User dropdown (shows org users — not role-based, user-specific)

## Custom Approval Configuration

When "Custom Approval" is selected:
- Shows "Custom Approval Workflows" heading
- Empty state: illustration + text "Tailor your Approval process with Custom Workflows"
- Description: "Create custom approval workflows based on multiple conditions to suit your business needs."
- CTA link: "New Custom Approval" → navigates to `#/settings/salary-revision/custom-approval/new?entity_type=salary_revision`

## Fields & Validations

| Field | Type | Required | Default | Options/Rules |
|-------|------|----------|---------|---------------|
| Approval Workflow type | Radio group | Yes | Simple Approval (implied) | Simple / Multi-Level / Custom |
| Level N Approver | User combobox | Yes (per level) | Current user | Org users list |

## Actions

| Action | Trigger | Pre-condition | Post-behavior |
|--------|---------|---------------|---------------|
| Select workflow type | Radio click | None | Shows relevant config UI |
| Add New Level | Button click | Multi-Level selected | Adds new level row |
| Delete level | Icon button | Level exists | Removes that level |
| New Custom Approval | Link | Custom selected | Navigates to custom approval builder |
| Save | Button click | Any workflow selected | Saves config (POST to API) |

## Business Rules
- Simple Approval: permission-based, any qualifying approver can unilaterally approve — no ordering
- Multi-Level: sequential, all levels must approve for the revision to proceed
- Custom: criteria-driven (criteria types not visible without creating a new rule — requires navigation to builder)
- No escalation timer/deadline configuration visible on this page
- No bypass/auto-approval option visible
- Approver is user-specific (not role-based) for Multi-Level — tight coupling to individual users

## Cross-Module Effects
- This setting determines the approval flow triggered when a salary revision is created from an employee's profile
- Affects Approvals module queue (where pending revisions appear)

## Gaps / Observations
- No "escalation after N days" configuration visible — if an approver is unavailable, the revision is stuck
- Approver assignment is user-specific, not role-based — risk of orphaned approvals if the approver leaves
- No bypass option for urgent salary revisions
- Custom approval builder criteria not inspected (separate page `#/settings/salary-revision/custom-approval/new`)
- No notification preference config on this page (email/in-app notification to approver not configurable here)

## Screenshots
- [Salary Revision Approval Settings](../screenshots/uf12-salary-revision-approval.png)
