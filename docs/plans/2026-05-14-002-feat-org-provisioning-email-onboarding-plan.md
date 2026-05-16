---
id: 2026-05-14-002
title: Org Provisioning — Admin Email + Welcome Email + Set-Password + Forgot Password + SMTP
status: active
created: 2026-05-14
area: auth, tenant, email
---

## Problem Frame

When a SuperAdmin provisions a new org, the org admin needs credentials to access their subdomain app. Currently the form has no email field and no delivery mechanism. This chunk adds:

1. Admin email field on provision form
2. Welcome email with set-password link to the org's subdomain URL
3. Set-password flow (public endpoint, uses ASP.NET Identity token)
4. Forgot password flow (always returns 200, no user enumeration)
5. SMTP configuration wired to real mail server (MailHog for dev, displayme SMTP for prod)

## Scope

**In scope:**
- `CreateTenantCommand` extended with `AdminEmail`
- OrgAdmin user created at provision time (no password, `RequirePasswordChange = false` sufficient — just set no password hash)
- Welcome email dispatched via Hangfire `notifications` queue
- `POST /api/auth/set-password` — public, validates ASP.NET Identity password-reset token
- `POST /api/auth/forgot-password` — public, always 200
- Frontend: provision form adds adminEmail, `SetPasswordPage`, `ForgotPasswordPage`, login page "Forgot password?" link

**Out of scope:**
- Email template HTML editor / branding per tenant
- Rate limiting on forgot-password (deferred — add with Redis sliding window in security hardening)
- Resend invite flow (deferred)
- Magic-link login (deferred)

## Key Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Email library | MailKit | Industry standard for .NET SMTP; supports STARTTLS on port 587; actively maintained |
| Token type for set-password | `UserManager.GeneratePasswordResetTokenAsync` | ASP.NET Identity built-in; time-limited; no custom token table needed |
| Token encoding | `WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token))` | URL-safe; matches ASP.NET Identity conventions |
| Email dispatch | Hangfire `notifications` queue via `IEmailJobDispatcher` | Keeps Application layer free of Hangfire dep; async fire-and-forget; consistent with arch rules |
| `IEmailJobDispatcher` placement | `Payroll.Application/Interfaces/` | Application layer owns the abstraction; Infrastructure implements |
| Forgot password enumeration guard | Always return 200 + same response body | Standard security practice — never reveal whether email exists |
| SMTP config source | `appsettings.json` `Email` section + Docker secrets override | Consistent with existing config patterns; secrets never in code |
| Dev SMTP | MailHog (already in docker-compose, port 1025) | Zero auth, catches all mail, existing infra |

## SMTP Configuration (appsettings)

```json
"Email": {
  "Host": "smtp.displayme.net",
  "Port": 587,
  "Username": "pitsmail",
  "Password": "R/A,4([6Q~=VZ(2N",
  "From": "pitsmail@smtp.displayme.net",
  "UseSsl": false,
  "UseStartTls": true,
  "BaseUrl": "http://localhost:5173"
}
```

Dev override in `appsettings.Development.json`:
```json
"Email": {
  "Host": "localhost",
  "Port": 1025,
  "Username": "",
  "Password": "",
  "From": "noreply@payroll.local",
  "UseSsl": false,
  "UseStartTls": false,
  "BaseUrl": "http://localhost:5173"
}
```

`BaseUrl` used to build: `https://{slug}.{base_domain}/set-password?token={encoded_token}`

For local dev the set-password URL is `http://localhost:5173/set-password?token=...&slug=...` (slug passed as query param so React frontend can use it).

## Architecture

```
Application layer:
  IEmailJobDispatcher (interface) — enqueue email jobs
  IEmailTemplateService (optional; start with inline templates)

Infrastructure layer:
  EmailSettings (strongly-typed config)
  SmtpEmailService : IEmailService (MailKit, sends actual mail)
  HangfireEmailJobDispatcher : IEmailJobDispatcher
  Jobs/SendWelcomeEmailJob
  Jobs/SendPasswordResetEmailJob

Api layer:
  AuthController — /api/auth/set-password, /api/auth/forgot-password
  TenantsController — /api/tenants already exists, extend CreateTenant handler
```

## Implementation Units

### U1 — Email Infrastructure

**Files:**
- `src/Payroll.Infrastructure/Email/EmailSettings.cs` — strongly-typed config record
- `src/Payroll.Infrastructure/Email/SmtpEmailService.cs` — MailKit implementation
- `src/Payroll.Application/Interfaces/IEmailService.cs` — send interface (Application owns abstraction)
- `src/Payroll.Application/Interfaces/IEmailJobDispatcher.cs` — dispatch interface
- `src/Payroll.Infrastructure/Email/HangfireEmailJobDispatcher.cs` — Hangfire implementation
- `src/Payroll.Infrastructure/DependencyInjection.cs` — register EmailSettings, SmtpEmailService, HangfireEmailJobDispatcher
- `src/Payroll.Api/appsettings.json` — add `Email` section (prod values)
- `src/Payroll.Api/appsettings.Development.json` — add `Email` section (MailHog values)
- `Directory.Packages.props` — add `MailKit` version entry

**IEmailService interface:**
```csharp
public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
```

**IEmailJobDispatcher interface:**
```csharp
public interface IEmailJobDispatcher
{
    void EnqueueWelcomeEmail(string toEmail, string orgSlug, string setPasswordUrl);
    void EnqueuePasswordResetEmail(string toEmail, string resetUrl);
}
```

**SmtpEmailService pattern** (from PLHB `email_service.py` — STARTTLS on port 587):
```csharp
using var smtp = new SmtpClient();
await smtp.ConnectAsync(settings.Host, settings.Port,
    settings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None, ct);
if (!string.IsNullOrEmpty(settings.Username))
    await smtp.AuthenticateAsync(settings.Username, settings.Password, ct);
await smtp.SendAsync(message, ct);
await smtp.DisconnectAsync(true, ct);
```

**Test scenarios:**
- `SmtpEmailService` connects with StartTLS when `UseStartTls = true`
- Skips auth when Username is empty (MailHog)
- Throws on SMTP connect failure (let Hangfire retry)

### U2 — Extend Tenant Provisioning

**Files:**
- `src/Payroll.Application/Commands/Platform/CreateTenantCommand.cs` — add `AdminEmail` property
- `src/Payroll.Application/Validators/Platform/CreateTenantCommandValidator.cs` — add email validation
- `src/Payroll.Application/Interfaces/IUserService.cs` — add `CreateOrgAdminAsync(string email, Guid tenantId, string role)`
- `src/Payroll.Infrastructure/Identity/UserService.cs` — implement `CreateOrgAdminAsync` (create user, no password, return user)
- `src/Payroll.Application/Commands/Platform/CreateTenantCommandHandler.cs` — call `CreateOrgAdminAsync`, then `EnqueueWelcomeEmail`
- `web/src/pages/platform/ProvisionOrgPage.tsx` — add `adminEmail` field to schema + form

**Handler flow:**
1. Create `Tenant` row (existing)
2. Provision schema (existing)
3. `await userService.CreateOrgAdminAsync(command.AdminEmail, tenant.Id, "OrgAdmin")`
4. Generate set-password token: `await userManager.GeneratePasswordResetTokenAsync(user)`
5. Build URL: `{BaseUrl.Replace("localhost:5173", $"{slug}.localhost:5173")}/set-password?token={encoded}&slug={slug}` — configurable via `Email:BaseUrl`
6. `emailJobDispatcher.EnqueueWelcomeEmail(command.AdminEmail, tenant.Slug, setPasswordUrl)`
7. Return `TenantDto` (existing)

**Validator additions:**
- `AdminEmail`: `NotEmpty()`, `EmailAddress()`, `MaximumLength(320)`

**Test scenarios:**
- Provision with valid email creates user with no password hash
- Provision with invalid email returns 400
- Provision dispatches exactly one welcome email job
- Duplicate email (409 from existing user) returns 409 with clear message

### U3 — Email Jobs

**Files:**
- `src/Payroll.Infrastructure/Jobs/SendWelcomeEmailJob.cs`
- `src/Payroll.Infrastructure/Jobs/SendPasswordResetEmailJob.cs`

**SendWelcomeEmailJob** — receives `(string toEmail, string orgSlug, string setPasswordUrl)`:
```
Subject: "Welcome to Indian Payroll — Set Your Password"
Body: HTML with org slug, set-password link, 72-hour expiry note
```

**SendPasswordResetEmailJob** — receives `(string toEmail, string resetUrl)`:
```
Subject: "Reset Your Password — Indian Payroll"
Body: HTML with reset link, 1-hour expiry note
```

Both jobs injected with `IEmailService`. Hangfire retries on failure (default 10 attempts, exponential backoff). Queue: `notifications`.

**HangfireEmailJobDispatcher:**
```csharp
public void EnqueueWelcomeEmail(string toEmail, string orgSlug, string setPasswordUrl)
    => BackgroundJob.Enqueue<SendWelcomeEmailJob>(
        q: "notifications",
        job => job.ExecuteAsync(toEmail, orgSlug, setPasswordUrl, CancellationToken.None));
```

**Test scenarios:**
- `SendWelcomeEmailJob.ExecuteAsync` calls `IEmailService.SendAsync` with correct to/subject
- `SendPasswordResetEmailJob.ExecuteAsync` calls `IEmailService.SendAsync` with reset URL in body
- (Mock `IEmailService` in job tests — real SMTP tested via manual MailHog inspection)

### U4 — Forgot Password Endpoint

**Files:**
- `src/Payroll.Application/Commands/Auth/ForgotPasswordCommand.cs` — `IRequest<Unit>`, `IAllowWithoutTenant`
- `src/Payroll.Application/Validators/Auth/ForgotPasswordCommandValidator.cs`
- `src/Payroll.Application/Commands/Auth/ForgotPasswordCommandHandler.cs`
- `src/Payroll.Api/Controllers/AuthController.cs` — add `POST /api/auth/forgot-password`

**Handler flow:**
1. Find user by email (`userManager.FindByEmailAsync`)
2. If user not found: return `Unit.Value` silently (no enumeration)
3. `await userManager.GeneratePasswordResetTokenAsync(user)`
4. Build reset URL
5. `emailJobDispatcher.EnqueuePasswordResetEmail(command.Email, resetUrl)`
6. Return `Unit.Value`

**Controller:**
```csharp
[AllowAnonymous] // platform-level — tenant context not available on forgot-password
[HttpPost("forgot-password")]
public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest req, CancellationToken ct)
{
    await mediator.Send(new ForgotPasswordCommand(req.Email), ct);
    return Ok(new { message = "If that email exists, a reset link has been sent." });
}
```

**Test scenarios:**
- Existing email: enqueues reset email, returns 200
- Non-existing email: no email enqueued, still returns 200 (no enumeration)
- Invalid email format: returns 400
- Always same response body regardless of user existence

### U5 — Set Password Endpoint

**Files:**
- `src/Payroll.Application/Commands/Auth/SetPasswordCommand.cs` — `IRequest<Unit>`, `IAllowWithoutTenant`
- `src/Payroll.Application/Validators/Auth/SetPasswordCommandValidator.cs`
- `src/Payroll.Application/Commands/Auth/SetPasswordCommandHandler.cs`
- `src/Payroll.Api/Controllers/AuthController.cs` — add `POST /api/auth/set-password`

**Request body:** `{ email, token, newPassword }`

**Handler flow:**
1. `user = await userManager.FindByEmailAsync(command.Email)` — return 400 if null
2. `decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(command.Token))`
3. `result = await userManager.ResetPasswordAsync(user, decodedToken, command.NewPassword)`
4. If `!result.Succeeded`: throw `ValidationException` with identity errors
5. Return `Unit.Value`

**Validator:**
- `Email`: required, valid email
- `Token`: required, non-empty
- `NewPassword`: `MinimumLength(8)`, must contain uppercase + lowercase + digit + special char (match Identity config)

**Test scenarios:**
- Valid token + email: password updated, returns 200
- Expired/invalid token: returns 400 with clear error
- Password too weak: returns 400 with validation errors
- Unknown email: returns 400 (token is bound to user — no enumeration risk here since token itself is secret)

### U6 — Frontend

**Files:**
- `web/src/pages/platform/ProvisionOrgPage.tsx` — add `adminEmail` field
- `web/src/pages/auth/SetPasswordPage.tsx` — new page
- `web/src/pages/auth/ForgotPasswordPage.tsx` — new page
- `web/src/pages/LoginPage.tsx` — add "Forgot password?" link
- `web/src/router.tsx` — add `/set-password`, `/forgot-password` as public routes

**ProvisionOrgPage additions:**
```tsx
adminEmail: z.string().min(1, 'Required').email('Invalid email'),
```
Field placed below `displayName`, above `slug`.

**SetPasswordPage** — reads `?token=&email=` from URL params:
- Form: `newPassword` + `confirmPassword` fields
- `onSubmit`: `POST /api/auth/set-password` with `{ email, token, newPassword }`
- On success: show "Password set. You can now log in." + link to login
- On error: show API error message

**ForgotPasswordPage:**
- Form: `email` field
- `onSubmit`: `POST /api/auth/forgot-password`
- Always show: "If that email exists, a reset link has been sent." (mirror backend)
- Link back to login

**LoginPage addition:**
```tsx
<Link to="/forgot-password" className="text-xs text-gray-500 hover:text-gray-700">
  Forgot password?
</Link>
```

**Router additions (public — no auth guard):**
```tsx
{ path: '/set-password', element: <SetPasswordPage /> },
{ path: '/forgot-password', element: <ForgotPasswordPage /> },
```

These sit outside both `RequireAuth` and `RequireSuperAdmin` wrappers — users arrive here unauthenticated.

**Test scenarios:**
- `SetPasswordPage` with valid token params renders form, submits, shows success
- `SetPasswordPage` with missing params shows error state
- `ForgotPasswordPage` always shows success message regardless of API response
- Forgot password link visible on login page

## Sequencing

```
U1 (Email infra)  → unblocks U2, U3
U2 (Extend provision) → depends on U1 (IEmailJobDispatcher)
U3 (Jobs) → depends on U1 (IEmailService, Hangfire)
U4 (Forgot password) → depends on U1 (dispatcher), independent of U2/U3
U5 (Set password) → independent of U1–U4 (only userManager needed)
U6 (Frontend) → depends on U2/U4/U5 endpoints existing
```

Recommended order: U1 → U5 → U4 → U2+U3 (parallel) → U6

## Dependencies

- `MailKit` — add to `Directory.Packages.props`
- `Microsoft.AspNetCore.WebUtilities` — already in `Microsoft.AspNetCore.App` framework ref; no extra package needed for `WebEncoders`
- Hangfire — already registered in `docker-compose.override.yml` and DI
- MailHog — already in `docker-compose.yml` on port 1025

## Risks

| Risk | Mitigation |
|---|---|
| ASP.NET Identity token tied to security stamp — invalidated on password change | Expected behavior; set-password flow is single-use by design |
| Subdomain URL construction for local dev (no actual subdomain) | Use `?slug=` query param in set-password URL for local dev; real subdomain in prod |
| MailKit STARTTLS negotiation fails with prod SMTP | Test with MailHog first; validate `SmtpEmailService` integration test with real SMTP separately |
| `CreateOrgAdminAsync` called in `CreateTenantCommandHandler` — platform-level, no schema context | Handler implements `IAllowWithoutTenant`; user created in public schema via platform db context |

## Test File Paths

| Unit | Test file |
|---|---|
| U1 SmtpEmailService | `tests/Payroll.Infrastructure.Tests/Email/SmtpEmailServiceTests.cs` |
| U2 CreateTenantCommandHandler | `tests/Payroll.Application.Tests/Commands/Platform/CreateTenantCommandHandlerTests.cs` |
| U3 SendWelcomeEmailJob | `tests/Payroll.Infrastructure.Tests/Jobs/SendWelcomeEmailJobTests.cs` |
| U3 SendPasswordResetEmailJob | `tests/Payroll.Infrastructure.Tests/Jobs/SendPasswordResetEmailJobTests.cs` |
| U4 ForgotPasswordCommandHandler | `tests/Payroll.Application.Tests/Commands/Auth/ForgotPasswordCommandHandlerTests.cs` |
| U5 SetPasswordCommandHandler | `tests/Payroll.Application.Tests/Commands/Auth/SetPasswordCommandHandlerTests.cs` |
| U4/U5 API integration | `tests/Payroll.Api.Tests/Controllers/AuthControllerTests.cs` |

## Patterns to Follow

- Command/handler pattern: see `src/Payroll.Application/Commands/Platform/CreateTenantCommand.cs` (existing)
- `IAllowWithoutTenant` marker: see `src/Payroll.Application/Commands/Platform/CreateTenantCommand.cs`
- Hangfire job registration: see `src/Payroll.Infrastructure/DependencyInjection.cs`
- Controller thin + MediatR send: see `src/Payroll.Api/Controllers/TenantsController.cs`
- FluentValidation pipeline: already registered in MediatR pipeline behaviour
- PLHB SMTP reference: `/home/abhi/PLHB/payroll-fastapi/app/services/email_service.py`
- PLHB set-password frontend: `/home/abhi/PLHB/payroll-frontend/src/features/auth/AcceptInvitePage.tsx`
