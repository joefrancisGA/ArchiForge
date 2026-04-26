# Hosted demo environment (public sandbox)

**Audience:** Platform operators shipping `demo.archlucid.net` (or another public demo host) for buyers and evaluators.

**Scope:** How the demo is wired, what data is pre-seeded, how to host it, cost ballpark, and how to reset on demand. This runbook is implementation-aligned with the repo; it is not a generic Azure tutorial.

## What the demo is

- **API + operator UI** in **DevelopmentBypass** (no sign-in) for the simplest evaluator path, or you may switch to **ApiKey** / **JwtBearer** for a stricter public posture.
- **Agent execution: Simulator** — deterministic, no Azure OpenAI cost.
- **Pre-seeded Contoso data** — on API startup, when `Demo:Enabled` and `Demo:SeedOnStartup` are `true`, `IDemoSeedService` runs after DbUp (`ArchLucid.Application/Bootstrap/DemoSeedService.cs`). It creates **two committed** Contoso Retail runs (baseline + hardened) plus governance/export fixtures, not three.
- **On-demand reset** — there is no automatic nightly reset. Wipe the database or re-provision the environment when you need a clean state.
- **New runs in the UI** — allowed in DevelopmentBypass; if you do not want public write traffic, use auth modes and `Demo:Enabled` / route policy appropriate for your threat model (not documented here as a product default).

## Local smoke (docker)

From the repo root:

```bash
docker compose -f docker-compose.yml -f docker-compose.demo.yml --profile demo-hosted up -d --build
```

- API: `http://localhost:5000`
- UI: `http://localhost:3000` (build passes `NEXT_PUBLIC_DEMO_URL=https://demo.archlucid.net` for the `/get-started` CTA; for same-origin demo, override the build arg to your local URL in a private override file).

Check health: `GET /health/ready` on the API.

## Marketing CTA

The marketing route `/get-started` shows **Try the live demo** when `NEXT_PUBLIC_DEMO_URL` is set at **Next.js build time** to an `https://` URL (see `archlucid-ui/src/app/(marketing)/get-started/page.tsx`). The Docker build supports `ARG NEXT_PUBLIC_DEMO_URL` in `archlucid-ui/Dockerfile`.

## Azure Container Apps (high level)

1. **Provision** a dedicated resource group, Azure SQL, Blob (Azurite is not for production; use a storage account for artifact offload if enabled), and Container Apps (API + UI) or App Service, matching your existing `infra/terraform-*` patterns.
2. **Connection string** to SQL, **DbUp** on API start (same as dev).
3. **Environment** (minimum for simulator demo):
   - `ASPNETCORE_ENVIRONMENT=Development` only if you intentionally use DevelopmentBypass; for anything internet-facing, prefer staging/production-like env with ApiKey or JwtBearer and **no** `DevelopmentBypass`.
   - `ArchLucid:StorageProvider=Sql`
   - `Demo:Enabled=true`, `Demo:SeedOnStartup=true` for seed (disable in true production if this host is shared with paid tenants).
   - `ArchLucid:AgentExecution:Mode=Simulator`
4. **UI image build** — pass `NEXT_PUBLIC_DEMO_URL=https://demo.archlucid.net` (or the UI origin if the marketing site and operator UI share one host) so the CTA is visible.
5. **Ingress** — TLS (Front Door or Container Apps ingress), WAF as required by your security policy.

**Rough monthly cost (order of magnitude):** a small **Azure SQL** + two **Container Apps** replicas + **Storage** is often in the **tens to low hundreds USD/month** for light demo traffic, depending on region, SKU, and data volume — align with your `infra` modules and cost alerts.

## URLs you can share after seeding

- Operator UI: `https://demo.archlucid.net/` (home)
- Runs list: `/runs?projectId=default` (or your tenant’s default project query)
- Pre-seeded run IDs: see `docs/TRUSTED_BASELINE.md` and `ContosoRetailDemoIdentifiers` for the canonical default-tenant run IDs in logs and docs.

## Related

- `docker-compose.demo.yml` — `demo-hosted` profile
- `docs/TRUSTED_BASELINE.md` — Contoso seed semantics
- `docs/BUILD.md` — build and configuration overview
