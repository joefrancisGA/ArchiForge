# ArchiForge → ArchLucid Rename Checklist

Incremental rename plan. One batch is folded into each working session.
See `.cursor/rules/ArchLucid-Rename.mdc` for the standing instruction.

> **Rule**: keep the build green after every batch. Never break CI.

---

## Phase 1 — Display text and documentation (low risk, any session)

- [x] 1.1 Update product name in `archiforge-ui/src/app/layout.tsx` (title, heading)
- [x] 1.2 Update `archiforge-ui/README.md` — product name references
- [x] 1.3 Update root `README.md` — product name, badges, description (2026-04-04)
- [x] 1.4 Update `docs/GLOSSARY.md` (2026-04-05)
- [x] 1.5 Update `docs/ARCHITECTURE_CONTEXT.md`, `docs/ARCHITECTURE_COMPONENTS.md`, `docs/ARCHITECTURE_CONTAINERS.md` (2026-04-05)
- [x] 1.6 Update `docs/DEPLOYMENT.md`, `docs/DEPLOYMENT_TERRAFORM.md`, `docs/CONTAINERIZATION.md` (2026-04-06 — ArchLucid product naming pass where referenced)
- [x] 1.7 Update `docs/PILOT_GUIDE.md`, `docs/OPERATOR_QUICKSTART.md`, `docs/RELEASE_SMOKE.md`, `docs/RELEASE_LOCAL.md` (2026-04-06 — ArchLucid product naming in pilot/release docs; literal `ArchiForge.*` paths and config keys unchanged)
- [x] 1.8 Update onboarding docs: `docs/onboarding/day-one-developer.md`, `day-one-sre.md`, `day-one-security.md`, `docs/CONTRIBUTOR_ONBOARDING.md`, `docs/ONBOARDING_HAPPY_PATH.md` (2026-04-06 — ArchLucid product naming in prose; repo folder, `ArchiForge.*` assemblies, and config keys unchanged)
- [ ] 1.9 Update `docs/BUILD.md`, `docs/FORMATTING.md`, `docs/TEST_STRUCTURE.md`, `docs/TEST_EXECUTION_MODEL.md`
- [ ] 1.10 Update remaining docs: ADRs, runbooks, CLI docs, changelogs, and any other `.md` files with stale references (batch as needed)
- [ ] 1.11 Update `archiforge-ui/docs/*.md` (ARCHITECTURE, COMPONENT_REFERENCE, TESTING_AND_TROUBLESHOOTING, OPERATOR_SHELL_TUTORIAL, etc.)
- [ ] 1.12 Update `.cursor/rules/Navigation.mdc` and `CSharp-EmbeddedStatements-NoBraces.mdc`

## Phase 2 — Configuration key bridges (medium risk, any session)

- [x] 2.1 Add fallback readers for `ArchiForgeAuth` → `ArchLucidAuth` in auth options binding (2026-04-05)
- [x] 2.2 Add fallback readers for `ArchiForge:StorageProvider` → `ArchLucid:StorageProvider` (2026-04-05)
- [ ] 2.3 Add fallback readers for any other `ArchiForge*` config sections (grep `appsettings*.json`)
- [ ] 2.4 Update `appsettings*.json` files to use new key names (old keys kept as comments for reference)
- [x] 2.5 Update `.env.example` — `ARCHLUCID_API_KEY` preferred + `ARCHIFORGE_API_KEY` fallback in proxy (2026-04-05)
- [ ] 2.6 Add OIDC storage key bridge reads (`archiforge_oidc_*` → `archlucid_oidc_*`) in `session.ts` and `storage-keys.ts`

## Phase 3 — UI directory and package rename (medium risk, any session)

- [ ] 3.1 Rename `archiforge-ui/` directory → `archlucid-ui/`
- [ ] 3.2 Update `package.json` name field
- [ ] 3.3 Update Dockerfile path references (API Dockerfile, docker-compose, CI workflows)
- [ ] 3.4 Update `archiforge-ui` references in root `README.md`, docs, and CI
- [ ] 3.5 Update TypeScript imports, API route env var names, and correlation header names

## Phase 4 — Infrastructure rename (medium-high risk, one stack per session)

- [ ] 4.1 `infra/terraform-monitoring/` — variable defaults and resource name strings
- [ ] 4.2 `infra/terraform-storage/` — variable defaults and resource name strings
- [ ] 4.3 `infra/terraform-edge/` — variable defaults, Front Door profile names, WAF policy names
- [ ] 4.4 `infra/terraform-container-apps/` — variable defaults, container app names
- [ ] 4.5 `infra/terraform-sql-failover/` — variable defaults, server/database names
- [ ] 4.6 `infra/terraform-openai/` — variable defaults, budget names
- [ ] 4.7 `infra/terraform-entra/` — app registration display names
- [ ] 4.8 `infra/terraform/` — APIM resource names, variable defaults
- [ ] 4.9 `infra/terraform-private/` — network resource names
- [ ] 4.10 Prometheus rule files (`infra/prometheus/archiforge-alerts.yml`, `archiforge-slo-rules.yml`) — rename files and content
- [ ] 4.11 Grafana dashboards (`infra/grafana/`) — rename files and dashboard titles
- [ ] 4.12 CI/CD workflows (`.github/workflows/*.yml`) — job names, image names, comments
- [ ] 4.13 `docker-compose.yml` — service names, image names
- [ ] 4.14 `stryker-config.json`, `coverage.runsettings`, `.devcontainer/devcontainer.json`

## Phase 5 — .NET project directory and file renames (high risk — ASK USER FIRST)

> **Do not start this phase without explicit user confirmation.**
> Strategy: rename directories and `.csproj` files but preserve `<RootNamespace>ArchiForge.*</RootNamespace>`
> so no C# source changes are needed yet. Update `.sln` and `<ProjectReference>` paths.

- [ ] 5.1 Rename leaf test projects: `*.Tests` directories and `.csproj` files
- [ ] 5.2 Rename `ArchiForge.TestSupport`, `ArchiForge.Benchmarks`
- [ ] 5.3 Rename `ArchiForge.Cli`, `ArchiForge.Cli.Tests`, `ArchiForge.Backfill.Cli`
- [ ] 5.4 Rename `ArchiForge.Api.Client`, `ArchiForge.Api.Client.Tests`
- [ ] 5.5 Rename domain projects: `Core`, `Contracts`, `Persistence`, `Application`, `AgentRuntime`, `AgentSimulator`
- [ ] 5.6 Rename domain projects: `Coordinator`, `Decisioning` (formerly separate `DecisionEngine`; now one assembly with `Merge`/`Validation` folders), `KnowledgeGraph`, `Retrieval`, `ContextIngestion`, `Provenance`, `ArtifactSynthesis`
- [ ] 5.7 Rename host projects: `Host.Core`, `Host.Composition`
- [ ] 5.8 Rename `ArchiForge.Api`, `ArchiForge.Worker`
- [ ] 5.9 Regenerate or edit `ArchiForge.sln` → `ArchLucid.sln`
- [ ] 5.10 Update `Directory.Build.props`
- [ ] 5.11 Update `templates/archiforge-finding-engine/` directory and contents
- [ ] 5.12 Rename C# files with `ArchiForge` prefix (23 files: `ArchiForgeConfigurationRules.cs`, `ArchiForgeInstrumentation.cs`, etc.)
- [ ] 5.13 Update NSwag config (`nswag.json`) and regenerate client

## Phase 6 — Bulk namespace rename (high risk — ASK USER FIRST)

> **Do not start this phase without explicit user confirmation.**
> This is a single large pass that updates all `namespace ArchiForge.*` and `using ArchiForge.*` directives.
> Remove `<RootNamespace>` overrides added in Phase 5.

- [ ] 6.1 Bulk rename all `namespace ArchiForge.*` → `namespace ArchLucid.*` across all `.cs` files
- [ ] 6.2 Bulk rename all `using ArchiForge.*` → `using ArchLucid.*`
- [ ] 6.3 Update XML doc `<see cref="ArchiForge.*">` references
- [ ] 6.4 Update string literals containing `"ArchiForge"` (Serilog source context, instrumentation, ProblemDetails type URIs)
- [ ] 6.5 Remove `<RootNamespace>` overrides from `.csproj` files (namespace now matches directory)
- [ ] 6.6 Full build + full test suite verification
- [ ] 6.7 Update `docs/NEXT_REFACTORINGS.md` to reflect completion

## Phase 7 — Cleanup and external (operational — ASK USER FIRST)

- [ ] 7.1 Remove config key fallbacks added in Phase 2 (old `ArchiForge*` keys)
- [ ] 7.2 Remove OIDC storage key bridge reads (old `archiforge_oidc_*` keys)
- [ ] 7.3 Remove environment variable fallbacks (old `ARCHIFORGE_*` vars)
- [ ] 7.4 Update SQL master DDL script (`ArchiForge.sql` → `ArchLucid.sql`); add a new migration for database rename if needed
- [ ] 7.5 Terraform `state mv` operations for renamed resources (coordinate with deploy window)
- [ ] 7.6 Rename GitHub repository
- [ ] 7.7 Update Entra ID app registrations (redirect URIs, display names)
- [ ] 7.8 Rename workspace root directory (`c:\ArchiForge\ArchiForge` → `c:\ArchLucid\ArchLucid`)
- [ ] 7.9 Final grep for any remaining `ArchiForge` or `archiforge` references; update this checklist

---

## Progress log

| Date | Batch | Notes |
|------|-------|-------|
| 2026-04-04 | 1.1 | layout.tsx title + heading → ArchLucid |
| 2026-04-05 | 1.2 | archiforge-ui/README.md product-facing name → ArchLucid (env keys unchanged) |
| 2026-04-05 | 1.4 | GLOSSARY.md title + ArchLucid note; outbox / integration dead-letter terms |
| 2026-04-05 | 2.1–2.2, 2.5 | Config bridges + `ResolveArchiForgeOptions` at early DI sites; UI proxy API key fallback; ops docs (architecture on a page, code map, config sunset, capacity/cost) |
| 2026-04-05 | 1.5 | Architecture context/components/containers titles + ArchLucid product framing; production cost guardrails (`production.tfvars.example`, LA quota note, WAF version var, FinOps playbook §9) |
| 2026-04-05 | (Improvement 5) | Merged `ArchiForge.DecisionEngine` into `ArchiForge.Decisioning` (later flattened to `ArchiForge.Decisioning.Merge` / `ArchiForge.Decisioning.Validation` — see 2026-04-06); added `ArchiForge.Persistence.Runtime` (orchestration, UoW, hot-path cache impls, blob store impls, archival); coordinator repos renamed to `ICoordinatorGoldenManifestRepository` / `ICoordinatorDecisionTraceRepository` in `Persistence.Data.Repositories`. |
| 2026-04-06 | Namespace flatten | Removed nested `Decisioning.DecisionEngine.*` namespaces: code under `ArchiForge.Decisioning/Merge/` and `ArchiForge.Decisioning/Validation/`; tests under `ArchiForge.Decisioning.Tests/Merge/` and `Validation/`; OTel meter `ArchiForge.Decisioning.SchemaValidation` (replaces `ArchiForge.DecisionEngine.SchemaValidation`). |
| 2026-04-06 | Quality batch (six improvements) | Stryker `break:60` (all `stryker-config*.json`); merged line-coverage gate documented in `docs/coverage-exclusions.md` (not Coverlet `<Threshold>` per assembly — see comment in `coverage.runsettings`); CLI `ResolveApiErrorMessage` for typed ProblemDetails; rename `DecisionTrace` → `RunEventTrace` (coordinator) / `RuleAuditTrace` (authority); `GET /v1/authority/runs/{id}/provenance`; UI `/runs/[id]/provenance`; `docs/DUAL_PIPELINE_NAVIGATOR.md`, `docs/CHAOS_TESTING.md`, `CliRetryDelegatingHandlerTests`. Prometheus SLO/outbox alerts already in `infra/prometheus/`. Phase 5–6 bulk project/namespace rename **not** started (explicit scope). |
| 2026-04-06 | 1.7 + CI coverage gate | Phase **1.7** pilot/release docs → ArchLucid product naming (`PILOT_GUIDE`, `OPERATOR_QUICKSTART`, `RELEASE_SMOKE`, `RELEASE_LOCAL`); CI **70%** merged line gate via `scripts/ci/assert_merged_line_coverage_min.py`; `RetrievalIndexingOutboxProcessor` uses `RunDetailDto.DecisionTrace` (fixes stale `RuleAuditTrace` property). |
