# Settings > Developer Data

## Navigation Location
Settings > EXTENSIONS & DEVELOPER DATA > Developer Data

## Sub-pages

| Sub-page | URL |
|---------|-----|
| Connections | `#/settings/developer-space/connections` |
| Incoming Webhooks | `#/settings/developer-space/incomingwebhooks` |
| Data Backup | `#/settings/data-backup` |

---

## Sub-page 1: Connections (`#/settings/developer-space/connections`)

### Purpose
Create OAuth connections to external REST APIs for use in Custom Buttons, Schedules, and Custom Functions. Enables Zoho Payroll to call any authenticated external API.

### Empty State
> "Use connections to connect Zoho Payroll with other applications, even ones without a direct integration. Connections can be used to invoke REST APIs of any application and get access to authenticated data from your custom buttons, schedules, and custom functions."

### Connections Table (empty)
| Column | Description |
|--------|-------------|
| Connections | My Connections (user-created) + Internal Connections (built-in Zoho-to-Zoho) |
| Description | My Connections: "Connections that you've created will be listed here." Internal: "Connections that are created with other Zoho products by built-in integration tasks in a deluge script, will be listed here." |

### Actions
| Button | Action |
|--------|--------|
| New Connection | Opens connection creation form (OAuth 2.0 or custom) |
| Page Tips | Contextual help |

---

## Sub-page 2: Incoming Webhooks (`#/settings/developer-space/incomingwebhooks`)

### Purpose
Create webhook endpoints that external applications can POST data to, triggering automated workflows within Zoho Payroll.

### Empty State
> "Incoming Webhooks allow you to receive information from other applications, so you can automate your payroll workflows and save time."

### Actions
| Button | Action |
|--------|--------|
| Add New | Opens incoming webhook creation form |
| View logs | Shows webhook invocation history |
| Add Incoming Webhooks | Same as Add New (in empty state body) |

---

## Business Rules

1. **Connections** — OAuth-based; can connect to any API with OAuth 2.0 or custom auth. Used within Deluge scripts (Custom Functions, Custom Buttons, Schedules).
2. **Internal Connections** — automatically created when Zoho-to-Zoho integrations (e.g., Zoho Books) are connected. Not user-configurable.
3. **Incoming Webhooks** — Zoho Payroll generates a unique URL; external system POSTs JSON payload to it; triggers a Deluge function.
4. **Deluge dependency** — all developer integrations (Custom Functions, Connections, Webhooks) require knowledge of Zoho's Deluge scripting language.

## Cross-Module Impact
| Feature | Payroll Impact |
|---------|----------------|
| Connections | Enables Custom Functions to call external APIs (e.g., fetch attendance from an HRMS) |
| Incoming Webhooks | Allows external triggers to initiate payroll actions (e.g., trigger salary revision from an ERP) |

## Observations & Notes
1. **Developer tools are Zoho-stack only** — all integrations use Deluge; no standard scripting (Python, JS, etc.). Creates tight Zoho vendor dependency.
2. **Webhook logs** available — "View logs" button for incoming webhooks provides invocation audit trail.
3. **No API documentation link visible** — no "Zoho Payroll API docs" link on this page; devs must find docs externally.
4. For our build: REST API with OpenAPI/Swagger spec. Incoming webhooks as a Hangfire job trigger. Outgoing connections via standard HTTP client (no Deluge lock-in). OAuth client credentials for third-party integrations.

## Screenshots
`docs/ba-audit/settings/screenshots/31-developer-data.png`
