---
title: "Docker build fails with NETSDK1064: stale obj/ artifacts and bad NuGet version pin"
date: 2026-05-14
category: build-errors
module: Docker / .NET Build Pipeline
problem_type: build_error
component: development_workflow
severity: high
root_cause: config_error
resolution_type: config_change
symptoms:
  - "NETSDK1064: Package AWSSDK.S3 version 3.7.414 was not found on NuGet"
  - "docker compose build --no-cache api fails with NuGet restore error"
  - "Build passes locally (dotnet build) but fails inside Docker container"
  - "Error persists after --no-cache rebuild when .dockerignore is absent"
tags: [docker, dotnet, netsdk1064, dockerignore, nuget, central-package-management, aws-sdk, build-context]
---

# Docker build fails with NETSDK1064: stale obj/ artifacts and bad NuGet version pin

## Problem

Docker build fails with `NETSDK1064: Package AWSSDK.S3 version 3.7.414 was not found`. The build succeeds locally because `dotnet restore` rewrites `project.assets.json` on every run, but inside Docker the stale `obj/` artifacts from the host machine override the fresh restore output — causing `dotnet publish --no-restore` to fail reading a lockfile referencing a non-existent package version.

## Symptoms

- `NETSDK1064: Package AWSSDK.S3 version 3.7.414 was not found` during `docker compose build api`
- `--no-cache` rebuild does not help — the `COPY src/ src/` layer copies `obj/` from the host, overwriting what `dotnet restore` just produced
- `dotnet build` works locally (restore runs and rewrites the lockfile)
- Adding `--no-restore` to `dotnet publish` in the Dockerfile makes the error appear immediately rather than silently using stale data

## What Didn't Work

- **`docker compose build --no-cache api`** — rebuilds all layers from scratch, but `COPY src/ src/` still copies the host's `obj/` directory. The stale `project.assets.json` in `obj/` references version 3.7.414, which overwrites the fresh restore output from step 9. Same error.

- **Fixing the NuGet version alone** — updating `Directory.Packages.props` to `AWSSDK.S3 4.0.23.1` resolves the version error on the next host `dotnet restore`, but without `.dockerignore` the Docker build still copies and re-applies the locally-generated `obj/` into the container. Correct version on a host with a clean restore works; wrong version on a host with no prior restore still fails.

- **`dotnet publish` without `--no-restore`** — masks the problem temporarily by re-running restore inside the publish step, but this skips the restore caching layer advantage and hides the root cause.

## Solution

Two changes together fix the issue. Both are required:

### 1. Fix the bad version pin in `Directory.Packages.props`

```xml
<!-- Before (version does not exist on NuGet) -->
<PackageVersion Include="AWSSDK.S3" Version="3.7.414" />

<!-- After -->
<PackageVersion Include="AWSSDK.S3" Version="4.0.23.1" />
```

AWSSDK.S3 `4.0.23.1` is the current stable release as of 2026-05.

### 2. Create `.dockerignore` at the repo root

```
**/bin/
**/obj/
**/.vs/
**/.git/
**/node_modules/
web/
tests/
docs/
graphify-out/
*.md
.env
.env.*
```

The critical entries are `**/bin/` and `**/obj/`. Without them, `COPY src/ src/` in the Dockerfile includes host-generated restore artifacts that overwrite the container's fresh restore output.

### How the Dockerfile layers interact

```dockerfile
# Step 9 — restore into fresh container layer (writes obj/project.assets.json)
RUN dotnet restore Payroll.Api/Payroll.Api.csproj

# Step 10 — WITHOUT .dockerignore, this overwrites the fresh obj/ with host artifacts
COPY src/ src/

# Step 11 — reads obj/project.assets.json, finds stale version reference, fails
RUN dotnet publish Payroll.Api/Payroll.Api.csproj --no-restore -c Release -o /app/publish
```

With `.dockerignore` excluding `**/obj/`, step 10 only copies source files. Step 11 reads the restore artifacts from step 9.

## Why This Works

**Bad version pin**: `AWSSDK.S3 3.7.414` was never published to NuGet — likely a typo during initial scaffold. Central Package Management (`Directory.Packages.props`) enforces this version across all projects, so even projects that don't directly reference AWSSDK fail to restore if the version is unresolvable.

**Stale obj/ override**: Docker build context is the entire repo directory by default. `COPY src/ src/` copies `src/**/obj/project.assets.json` from the host into the container image. These files reference whatever versions were on the host at the time of the last `dotnet restore`. When a subsequent Docker layer runs `dotnet publish --no-restore`, it reads the overwritten (host) lockfile instead of the freshly-restored container lockfile. The `--no-restore` flag instructs MSBuild to trust the lockfile as-is, so any version reference in the overwritten file is taken literally — even if that version doesn't exist.

Together: the bad version in `Directory.Packages.props` only manifests inside Docker because the stale `obj/` artifacts are copied before publish. On the host, `dotnet build` runs a full restore first, so the bad version is resolved and the lockfile is rewritten with whatever NuGet finds (in this case, nothing — causing a host error too if you wipe `obj/`).

## Prevention

1. **Create `.dockerignore` as part of every new .NET scaffold** — treat it the same as `.gitignore`. The initial scaffold for this project was missing it. (session history)

2. **Verify package versions against NuGet before committing `Directory.Packages.props`** — run `dotnet restore` from scratch (`rm -rf src/**/obj && dotnet restore`) after adding a new version pin. A version that resolves locally with a cached `obj/` may not exist on NuGet.

3. **Use `dotnet restore --locked-mode` in CI and Docker** — this flag fails the restore if `packages.lock.json` is stale, making version resolution errors loud and immediate rather than silent.

4. **Add a CI step that builds the Docker image** — local `dotnet build` succeeds even with a bad version pin if `obj/` is already populated. A Docker build starts clean and catches packaging errors the local build masks.

5. **Check AWSSDK major version when scaffolding** — the AWS SDK for .NET has two active major versions (3.x and 4.x) with different package IDs and version ranges. Verify the correct major version on [NuGet](https://www.nuget.org/packages/AWSSDK.S3) before adding to `Directory.Packages.props`.

## Related Issues

- No existing docs/solutions entries were found for this problem — first occurrence in this repo. (session history)
