# PLHB — Deployment (Azure DevOps + Kubernetes)

This folder lets the DevOps team deploy **PLHB** (this repo — a .NET 8 API +
React SPA + Hangfire worker) in place of the old microservice repos, reusing the
existing cluster, registry, agent pool, Postgres-17, and Redis.

## What PLHB replaces

| Old repo (retire) | PLHB artifact |
|---|---|
| `payroll-backend` | API image — `src/Payroll.Api/Dockerfile.ci` → `CI-CD/K8s-Api.yml` |
| backend Hangfire | Worker Deployment in `CI-CD/K8s-Api.yml` (same image, `Hangfire__WorkerOnly=true`) |
| `payroll-frontend` | Web image — `web/Dockerfile.ci` → `CI-CD/K8s-Web.yml` |
| `payroll-fastapi`, `payroll-platform` | **Dropped** — PLHB is self-contained (no FastAPI, no separate control-plane service) |

Reused unchanged: cluster, `$(namespace)`, agent pool `PLHB-BE`, registry service
connection `Docker_Reg` (`docker-registry.dev.displayme.net`), base image
`pitsdevops.azurecr.io/pits-linux-base`, ingress host, Postgres-17, Redis.

## Build pipelines (build + push only)

Two pipelines, matching the existing per-service pattern (`trigger: develop`,
pool `PLHB-BE`, `replacetokens@5` on `.env.example`, `Docker@2` build+push):

- `CI-CD/AzurePipeline-Api-Dev.yml`  — variable group **`PLHB-Api-DEV`**
- `CI-CD/AzurePipeline-Web-Dev.yml`  — variable group **`PLHB-Web-DEV`**

Each pushes to `docker-registry.dev.displayme.net/$(dockerrepo)`. They do **not**
deploy — apply the K8s manifests separately (release pipeline / `kubectl`), same
as today.

## Deploy

1. Ensure Postgres-17 has the database (`POSTGRES_DB`) and a user. The app creates
   its own schemas (public + per-tenant) and runs EF migrations on API startup —
   no migration job needed.
2. Apply API + worker + storage:  `envsubst` / token-replace `CI-CD/K8s-Api.yml`, then `kubectl apply -f`.
3. Apply web + ingress:           token-replace `CI-CD/K8s-Web.yml`, then `kubectl apply -f`.

## Variable groups

Create two groups. **Mark every password/key/secret as secret.** All keys are
documented (with the token form) in the repo-root `.env.example`.

**`PLHB-Api-DEV`** (API + worker + K8s-Api.yml):
`dockerrepo`, `namespace`, `proj_branch`, `site_url`, `STORAGE_CLASS`,
`POSTGRES_HOST`, `POSTGRES_PORT`, `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`,
`REDIS_HOST`, `REDIS_PORT`, `REDIS_PASSWORD`,
`ENCRYPTION_KEY`, `SUPERADMIN_EMAIL`, `SUPERADMIN_PASSWORD`, `OPENIDDICT_CLIENT_SECRET`,
`SMTP_HOST`, `SMTP_PORT`, `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_FROM`, `SMTP_USE_SSL`, `SMTP_USE_STARTTLS`

**`PLHB-Web-DEV`** (web + K8s-Web.yml):
`dockerrepo`, `namespace`, `proj_branch`, `site_url`, `API_HOST`

> `API_HOST` must equal the API Service name — i.e. the `PLHB-Api-DEV` group's
> `<proj_branch>-api`. The web and API manifests resolve from separate variable
> groups, so this link is explicit (not derived) to avoid a silent `/api` 502.

> The old backend's variables (`JWT_SECRET`, `FastApi__*`, `POSTGRES_DB_DOTNET`,
> `POSTGRES_DB_HANGFIRE`, `MultiTenancy__*`) do **not** apply to PLHB. PLHB uses
> OpenIddict (not JWT secrets), schema-per-tenant in one DB (not separate dotnet/
> hangfire/platform DBs), and a PVC for files (not MinIO/S3).

## Caveats (read before first deploy)

- **RWX storage required.** `STORAGE_CLASS` must support `ReadWriteMany` — the API
  and worker share one PVC (`$(proj_branch)-storage`) for payslips / Form16 / logos.
  If no RWX class exists, either provision one (azurefile/nfs) or co-locate API +
  worker on one node and switch the PVC to `ReadWriteOnce`.
- **Migrations race on every deploy.** Both the API and the worker run platform
  EF `MigrateAsync` on startup (the worker only skips per-*tenant* provisioning, not
  the public-schema migrate), so they race the public schema on each rollout —
  `replicas: 1` does **not** prevent this. The app's startup is wrapped to tolerate
  it, but the robust fix is deploy ordering (apply/ready the API before the worker)
  or an init-container that runs migrations once under a lock. Likewise keep
  `replicas: 1` per deployment until such a strategy exists.
- **Startup can take minutes on a populated DB.** Kestrel binds `:8080` only after
  migrations + per-tenant provisioning finish; a `startupProbe` (up to ~5 min) gates
  liveness so a slow first boot doesn't crash-loop. Raise `failureThreshold` if you
  have very many tenants.
- **CORS is same-origin by design.** The web nginx proxies `/api`, `/connect`,
  `/health` to the API Service, so the browser only talks to `site_url`. The API's
  Production CORS allowlist is hardcoded to localhost and is never exercised in this
  topology. Do **not** expose the API on a separate public host without first making
  that allowlist configurable.
- **`pits-linux-base` images.** The CI Dockerfiles install the .NET SDK/runtime and
  Node on `pits-linux-base` to guarantee they build on the `PLHB-BE` pool. If those
  agents have public registry access, the slimmer dev Dockerfiles
  (`src/Payroll.Api/Dockerfile`, `web/Dockerfile`, `mcr`/`node`/`nginx` bases) can be
  used instead.
