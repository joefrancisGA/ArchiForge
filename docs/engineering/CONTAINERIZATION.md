> **Scope:** Containerization for **ArchLucid contributors and internal operators** - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.

> **Audience banner — read first.** ArchLucid is a **SaaS** product. **Customer-facing deliverables are the public website, the in-product operator UI, the published API client libraries, OpenAPI contracts, and documentation.** Docker images, `docker compose` definitions, and the contents of this document are **engineering / CI/CD / vendor-operations artifacts** — not a "customer installs our containers" distribution model. Customer entry points: **[`START_HERE.md`](../START_HERE.md)** "Audience split" and `archlucid.com`.

# Containerization

## Objective

Provide production-ready Docker images for the **ArchLucid** API and Operator UI that are identical across local integration testing and **vendor-operated** cloud deployment (build once, run in CI and in the service operator’s registry).

## Customer product boundary (SaaS)

ArchLucid is a **vendor-operated SaaS** product. **Customer-facing deliverables** are the **public website**, the **in-product operator UI**, **published API client libraries**, **OpenAPI contracts**, and **documentation** — not a program to **ship production container images or Helm charts to customers** as the licensed product (see **`docs/PENDING_QUESTIONS.md`** Resolved, 2026-04-21).

Images and **`docker compose`** definitions in this repository exist for **engineering**, **CI/CD**, **security scanning**, and **optional internal-operator demos** (including seller-led demos). They are **development and operations artifacts**, not a committed “customer installs our stack from a container tarball” distribution model unless product strategy reopens that path in a future ADR.

## Assumptions

- Docker Desktop (or equivalent) is installed locally (contributor / internal-operator only).
- .NET 10 SDK and Node 22 are the current build toolchains.
- Azure Container Registry (ACR) is the intended production image store; provision a registry in your subscription and wire **`ACR_LOGIN_SERVER`** (and related secrets) on the GitHub **staging** / **production** environments for **[DEPLOYMENT_CD_PIPELINE.md](../library/DEPLOYMENT_CD_PIPELINE.md)**.
- The reference Azure path is **Container Apps** via **`infra/terraform-container-apps`**; these Dockerfiles remain target-agnostic for other hosts.

## Constraints

- Images must run as non-root users (WAF SE:02).
- No SDK, dev dependencies, or test code in runtime images (WAF SE:02, CO:07).
- Health probes must be present for orchestrator self-healing (WAF RE:06).
- SMB (port 445) must not be exposed publicly (workspace security rule).

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│  docker compose --profile full-stack up                             │
│                                                                     │
│  ┌──────────┐  ┌──────────┐  ┌───────┐  ┌───────────┐  ┌────────┐ │
│  │ SQL      │  │ Azurite  │  │ Redis │  │ API       │  │ UI     │ │
│  │ Server   │  │ (blob/   │  │       │  │ :8080     │  │ :3000  │ │
│  │ :1433    │  │  queue/  │  │ :6379 │  │ Alpine    │  │ Alpine │ │
│  │          │  │  table)  │  │       │  │ non-root  │  │ non-   │ │
│  │          │  │          │  │       │  │           │  │ root   │ │
│  └──────────┘  └──────────┘  └───────┘  └─────┬─────┘  └───┬────┘ │
│       ▲                                       │             │      │
│       └───────────────────────────────────────┘             │      │
│                                                 proxy ──────┘      │
└─────────────────────────────────────────────────────────────────────┘
```

## Development Workflows

### Workflow 1 — Hot-reload (default)

Run only infrastructure in Docker; run API and UI natively for fast iteration.

```bash
docker compose up -d              # SQL, Azurite, Redis
dotnet run --project ArchLucid.Api
cd archlucid-ui && npm run dev
```

This is unchanged from before containerization was added.

### Workflow 2 — Full-stack integration

Run everything in containers to validate the production image locally.

```bash
docker compose --profile full-stack up -d --build
```

| Service | Host port | Container port | Image |
|---------|-----------|----------------|-------|
| SQL Server | 1433 | 1433 | `mcr.microsoft.com/mssql/server:2022-latest` |
| Azurite | 10000–10002 | 10000–10002 | `mcr.microsoft.com/azure-storage/azurite:latest` |
| Redis | 6379 | 6379 | `redis:7-alpine` |
| API | 5000 | 8080 | `archlucid-api` (local build) |
| UI | 3000 | 3000 | `archlucid-ui` (local build) |

### Workflow 2a — Demo overlay (internal-operator demos only)

Additive file: **`docker-compose.demo.yml`** (does not replace **`docker-compose.yml`**). Sets **`Demo:Enabled`**, **`Demo:SeedOnStartup`**, and **`AgentExecution:Mode=Simulator`** on the API service so Contoso demo data seeds after DbUp and agents run without cloud LLM calls. Use with the **full-stack** profile.

```bash
docker compose -f docker-compose.yml -f docker-compose.demo.yml --profile full-stack up -d --build
```

Or run **`scripts/demo-start.ps1`** / **`scripts/demo-stop.ps1`** (Windows) or **`scripts/demo-start.sh`** / **`scripts/demo-stop.sh`** (macOS/Linux). Internal seller-led walkthrough: **[go-to-market/DEMO_QUICKSTART.md](../go-to-market/DEMO_QUICKSTART.md)** (this is **not** the customer first-run path; customers use the trial funnel on `archlucid.com`).

### Workflow 3 — Azure deployment

Same images pushed to ACR. **First-time** provisioning uses **Terraform** under `infra/terraform-*` (see **`docs/library/DEPLOYMENT_TERRAFORM.md`**). **Ongoing releases** typically use **GitHub Actions CD** (`.github/workflows/cd.yml`) to push tags and `az containerapp update` the API, worker (same API image), and UI—see **`docs/library/DEPLOYMENT_CD_PIPELINE.md`**. The Dockerfiles do not change between those paths.

---

## Building Images Individually

### API

```bash
# From the repository root (the API Dockerfile needs sibling project references)
docker build -f ArchLucid.Api/Dockerfile -t archlucid-api .
```

### UI

```bash
# From the archlucid-ui directory (self-contained Next.js project)
docker build -t archlucid-ui archlucid-ui/
```

---

## Component Breakdown

### API Dockerfile (`ArchLucid.Api/Dockerfile`)

| Stage | Base image | Purpose |
|-------|-----------|---------|
| `restore` | `mcr.microsoft.com/dotnet/sdk:10.0.201-alpine3.23` (pinned SDK band + Alpine; bump with `global.json` / CI) | Copy `.csproj` files and run `dotnet restore … -r linux-musl-x64` (RID required for later `publish --no-restore -r linux-musl-x64`; avoids NETSDK1047) |
| `publish` | (extends `restore`) | Copy source, re-restore with same RID, then `dotnet publish -c Release -r linux-musl-x64 --no-restore` (API + Worker → `/app`) |
| `runtime` | `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` (ASP.NET **runtime** tag — not the SDK `10.0.201` patch) | Non-root user, health check, port 8080 |

### UI Dockerfile (`archlucid-ui/Dockerfile`)

| Stage | Base image | Purpose |
|-------|-----------|---------|
| `deps` | `node:22-alpine` | `npm ci` (layer-cached on `package-lock.json`) |
| `build` | `node:22-alpine` | `next build` with `output: "standalone"` |
| `runtime` | `node:22-alpine` | Non-root user, health check, port 3000 |

The `standalone` output mode copies only the required subset of `node_modules` into `.next/standalone`, producing images roughly 10× smaller than copying the full `node_modules`.

---

## Security Model

| Control | Implementation |
|---------|---------------|
| Non-root user | Both images create an `archlucid` user/group; `USER archlucid` before `ENTRYPOINT`/`CMD` |
| No dev dependencies | Multi-stage builds discard SDK, test tools, and full `node_modules` |
| No secrets in image | `.dockerignore` excludes `.env*`, credentials; secrets are injected at runtime via environment variables or mounted config |
| Minimal OS surface | Alpine base images (~5 MB OS layer) |
| Health probes | `HEALTHCHECK` instructions use `wget` against the liveness endpoint |

---

## Operational Considerations

### Health checks

| Service | Endpoint | Meaning |
|---------|----------|---------|
| API | `GET /health/live` | Process is running |
| API | `GET /health/ready` | Database + schema + compliance packs are available |
| API | `GET /health` | All checks |
| UI | `GET /` | Next.js server is responding |

Docker's `HEALTHCHECK` instruction uses the liveness endpoint. Orchestrators (App Service, AKS) should map readiness probes to `/health/ready`.

The **`docker-compose.yml` full-stack profile** also defines **`healthcheck`** blocks for **`api`** and **`ui`** (mirroring the image probes) so **`ui`** can **`depends_on`** **`api`** with **`condition: service_healthy`**, reducing startup races when both containers start together.

### Environment variables

Runtime configuration is injected via environment variables. The `docker-compose.yml` full-stack profile shows the development defaults. For Azure, these come from App Service configuration, Key Vault references, or Kubernetes secrets.

### Image sizes (approximate)

| Image | Expected size |
|-------|--------------|
| `archlucid-api` (Alpine + .NET runtime + published app) | ~120–150 MB |
| `archlucid-ui` (Alpine + Node + standalone output) | ~80–120 MB |

### Layer caching

Both Dockerfiles are structured so that dependency installation (NuGet restore / `npm ci`) is cached independently of source code changes. Rebuilds after code-only changes skip the dependency layer entirely.

### `.dockerignore`

The repository root `.dockerignore` is used by the API build context (which is the repo root). The `archlucid-ui/.dockerignore` is used by the UI build context (which is the `archlucid-ui` directory).

---

## What This Does NOT Cover

| Item | Status | Notes |
|------|--------|-------|
| Azure Container Registry (ACR) | Operator-owned | Create per environment; Terraform variables reference registry URL / identity |
| Terraform roots | Available | `infra/terraform-container-apps`, `infra/terraform-storage`, `infra/terraform-private`, `infra/terraform-edge`, `infra/terraform-monitoring`, `infra/terraform-entra`, `infra/terraform-sql-failover`, `infra/terraform-openai`, optional `infra/terraform` (APIM) — see **`docs/library/DEPLOYMENT_TERRAFORM.md`** |
| CI/CD image build + push | Partial | `.github/workflows/ci.yml` builds images and can be extended to push to ACR |
| VNet / private endpoints | Terraform | `infra/terraform-private` and related modules (landing-zone dependent) |
| Managed Identity | Terraform / host | Wire in Container Apps / App Service modules per environment |
| TLS / custom domain | Terraform | `infra/terraform-edge` (Front Door pattern) |
| Image vulnerability scanning | CI (Trivy) | `.github/workflows/ci.yml` runs **Trivy** on API and UI images (CRITICAL/HIGH) and **Trivy IaC** on Terraform; tune severities and registry push gates in the workflow |

Remaining gaps (if any) are organizational: subscription placement, naming, and which Terraform roots you enable per stage.
