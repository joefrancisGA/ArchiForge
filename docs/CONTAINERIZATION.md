# Containerization

## Objective

Provide production-ready Docker images for the ArchiForge API and Operator UI that are identical across local integration testing and cloud deployment.

## Assumptions

- Docker Desktop (or equivalent) is installed locally.
- .NET 10 SDK and Node 22 are the current build toolchains.
- Azure Container Registry (ACR) will be the production image store (not yet provisioned).
- The deployment target (App Service containers, ACI, AKS) has not been finalised; these Dockerfiles are target-agnostic.

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
dotnet run --project ArchiForge.Api
cd archiforge-ui && npm run dev
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
| API | 5000 | 8080 | `archiforge-api` (local build) |
| UI | 3000 | 3000 | `archiforge-ui` (local build) |

### Workflow 3 — Azure deployment (future)

Same images pushed to ACR, deployed via Terraform to the chosen compute target. The Dockerfiles do not change; only the infrastructure-as-code layer wraps them.

---

## Building Images Individually

### API

```bash
# From the repository root (the API Dockerfile needs sibling project references)
docker build -f ArchiForge.Api/Dockerfile -t archiforge-api .
```

### UI

```bash
# From the archiforge-ui directory (self-contained Next.js project)
docker build -t archiforge-ui archiforge-ui/
```

---

## Component Breakdown

### API Dockerfile (`ArchiForge.Api/Dockerfile`)

| Stage | Base image | Purpose |
|-------|-----------|---------|
| `restore` | `mcr.microsoft.com/dotnet/sdk:10.0-alpine` | Copy `.csproj` files and run `dotnet restore` (layer-cached) |
| `publish` | (extends `restore`) | Copy source, run `dotnet publish -c Release` |
| `runtime` | `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` | Non-root user, health check, port 8080 |

### UI Dockerfile (`archiforge-ui/Dockerfile`)

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
| Non-root user | Both images create an `archiforge` user/group; `USER archiforge` before `ENTRYPOINT`/`CMD` |
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
| `archiforge-api` (Alpine + .NET runtime + published app) | ~120–150 MB |
| `archiforge-ui` (Alpine + Node + standalone output) | ~80–120 MB |

### Layer caching

Both Dockerfiles are structured so that dependency installation (NuGet restore / `npm ci`) is cached independently of source code changes. Rebuilds after code-only changes skip the dependency layer entirely.

### `.dockerignore`

The repository root `.dockerignore` is used by the API build context (which is the repo root). The `archiforge-ui/.dockerignore` is used by the UI build context (which is the `archiforge-ui` directory).

---

## What This Does NOT Cover

| Item | Status | Notes |
|------|--------|-------|
| Azure Container Registry (ACR) | Not provisioned | Needed before Azure deployment |
| Terraform modules | Not created | Depends on deployment target decision |
| CI/CD image build + push | Not configured | GitHub Actions job to build, tag, push to ACR |
| VNet / private endpoints | Not configured | Required for production SQL, storage access |
| Managed Identity | Not configured | For ACR pull, Key Vault access, SQL auth |
| TLS / custom domain | Not configured | Front Door or App Gateway handles termination |
| Image vulnerability scanning | Not configured | Trivy or Defender for Containers recommended |

These are infrastructure concerns, addressed by future Terraform and CI change sets.
