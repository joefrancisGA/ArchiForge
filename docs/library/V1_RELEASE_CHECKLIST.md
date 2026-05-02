> **Scope:** ArchLucid V1 — release checklist - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid V1 — release checklist

**Audience:** release owner, SRE, and pilot program leads cutting a **V1** build or environment.

**How to use:** Work top to bottom. Check boxes when the item is **done for this release** (build ID / environment recorded in your run notes). This is **operational**, not a substitute for full automated CI.

**Scope:** Aligned with [V1_SCOPE.md](V1_SCOPE.md). **Automated gates:** [RELEASE_LOCAL.md](RELEASE_LOCAL.md), [RELEASE_SMOKE.md](RELEASE_SMOKE.md), [TEST_STRUCTURE.md](TEST_STRUCTURE.md). **RC environment drill (API already running):** [V1_RC_DRILL.md](V1_RC_DRILL.md) and **`v1-rc-drill.ps1`**.

---

## 1. Scope freeze

- [ ] **V1 scope** is unchanged or [V1_SCOPE.md](V1_SCOPE.md) is updated with date + notes (no silent drift).
- [ ] **Out-of-scope items** for V1.1+ are acknowledged in release notes or internal deferral list (see V1 scope §3).
- [ ] **OpenAPI / contract** snapshot or API version string reviewed if you ship client bundles ([API_CONTRACTS.md](API_CONTRACTS.md)).
- [ ] **Optional features** (integration events, webhooks, governance-heavy paths) are either **off**, **on with config documented**, or **explicitly excluded** from this release’s “supported surface” statement.

---

## 2. Deployment readiness

- [ ] **Release build** succeeds: `build-release.ps1` (or `dotnet build ArchLucid.sln -c Release`) per [RELEASE_LOCAL.md](RELEASE_LOCAL.md).
- [ ] **Merge-blocking .NET full regression (SQL)** on default branch is green before declaring release-ready — job **`.NET: full regression (SQL)`** (`dotnet-full-regression`) in `.github/workflows/ci.yml`; merged Cobertura + package-line gates per [CODE_COVERAGE.md](CODE_COVERAGE.md) and [TEST_EXECUTION_MODEL.md](TEST_EXECUTION_MODEL.md). If CI is red, record the blocking failure; do not imply "clean regression" without that job.
- [ ] **Readiness script** green for the agreed filter: `run-readiness-check.ps1` (Phase 2 now runs **`dotnet run … -- config lint`**; use `-SkipUi` only if UI is out of scope for this handoff).
- [ ] **Smoke with SQL** (when V1 includes Sql persistence): `release-smoke.ps1` with **`ARCHLUCID_SMOKE_SQL`** (or **`ConnectionStrings__ArchLucid`**) or `-SqlConnectionString` — see [RELEASE_SMOKE.md](RELEASE_SMOKE.md).
- [ ] **RC drill** (staged/prod-like API URL): run **`v1-rc-drill.ps1`** against the candidate deployment or run the manual steps in [V1_RC_DRILL.md](V1_RC_DRILL.md) (two runs, compare, authority replay, export ZIP, support bundle).
- [ ] **Staging evidence artifact** captured: `.\capture-staging-readiness-evidence.ps1 -BaseUrl https://<staging-host> -AuthMode <mode>`; add `-RunDoctor` / `-RunRcDrill` when the target allows those checks. Store the generated `artifacts/staging-readiness/*.md` with release artifacts, not in git.
- [ ] *(Optional SaaS fleets)* **Reliability drill** automation understood if you consume the scheduled workflow output — [RELIABILITY_DRILL_PACKAGE.md](../runbooks/RELIABILITY_DRILL_PACKAGE.md).
- [ ] **Package handoff** (if distributing bits): `package-release.ps1`; verify `artifacts/release/` contains **`metadata.json`**, **`PACKAGE-HANDOFF.txt`**, and checksums when required ([RELEASE_LOCAL.md](RELEASE_LOCAL.md)).
- [ ] **Runtime config** documented for target environment: connection string key (**`ConnectionStrings:ArchLucid`** or **`ArchLucid`** per bridge), **`ArchLucid:StorageProvider`** / **`ArchLucid:StorageProvider`**, **`ArchLucidAuth`** / **`ArchLucidAuth`**, agent mode (**`AgentExecution:Mode`**) ([README.md](../../README.md), [BUILD.md](BUILD.md)).
- [ ] **Containers** (if used): image tags recorded; compose profile documented ([CONTAINERIZATION.md](CONTAINERIZATION.md)).
- [ ] **Migrations:** DbUp applies cleanly on a fresh DB and on upgrade from **previous supported** schema ([SQL_SCRIPTS.md](SQL_SCRIPTS.md)).

---

## 3. Health and diagnostics

- [ ] **`GET /health/live`** returns success on running instances.
- [ ] **`GET /health/ready`** reflects real dependencies (e.g. Sql when not InMemory; schema files, compliance pack, temp dir as configured) ([README.md](../../README.md)).
- [ ] **`GET /version`** (or health JSON **`version`** / **`commitSha`**) matches **`metadata.json`** / git expectation for this build ([PILOT_GUIDE.md](PILOT_GUIDE.md)).
- [ ] **`dotnet run --project ArchLucid.Cli -- doctor`** succeeds against the deployed/staged API base URL (or equivalent in packaged deployment).
- [ ] **Correlation:** sample request with **`X-Correlation-ID`** appears in logs as expected ([API_CONTRACTS.md](API_CONTRACTS.md)).
- [ ] **Observability** (if production): metrics/logs dashboards or queries smoke-tested for one synthetic run ([OPERATIONS_ADMIN.md](OPERATIONS_ADMIN.md) if applicable).
- [ ] **Agent output quality corpus** recorded: `python scripts/ci/eval_agent_corpus.py --markdown-report artifacts/agent-output-quality.md --enforce-quality-gate` for tagged release candidates, or documented as skipped for simulator-only internal builds.
- [ ] **Real-LLM credibility (when ship posture includes real AOAI):** at least one session logged per [REAL_LLM_RUN_EVIDENCE_TEMPLATE.md](../quality/REAL_LLM_RUN_EVIDENCE_TEMPLATE.md) and [MANUAL_QA_CHECKLIST.md](../quality/MANUAL_QA_CHECKLIST.md) §8.3 — **not** required when the release is simulator-only end-to-end.

---

## 4. Guided operator flow validation

Execute the **core path** from [V1_SCOPE.md](V1_SCOPE.md) §4 (or [PILOT_GUIDE.md](PILOT_GUIDE.md) Option A/B):

- [ ] **Create run** — `POST /v1/architecture/request` (or CLI path) succeeds; **`runId`** captured.
- [ ] **Execute** — `POST /v1/architecture/run/{runId}/execute` (or environment’s equivalent) completes to a committable state.
- [ ] **Commit** — `POST /v1/architecture/run/{runId}/commit` succeeds; **`409`** behavior understood if retried wrong state ([API_CONTRACTS.md](API_CONTRACTS.md)).
- [ ] **Artifacts** — at least one artifact descriptor for committed **`goldenManifestId`** (API list or CLI `artifacts`).
- [ ] **Operator UI** (if in scope): open run → manifest/artifacts **Review** / **Download** works ([operator-shell.md](operator-shell.md)).
- [ ] **Compare** — two-run compare produces an expected structured or legacy diff in UI or API ([COMPARISON_REPLAY.md](COMPARISON_REPLAY.md)).
- [ ] **Replay** — run replay or comparison replay path exercised per your pilot script ([ARCHITECTURE_FLOWS.md](ARCHITECTURE_FLOWS.md)).
- [ ] **Graph** — load graph for one **run ID** in UI ([operator-shell.md](operator-shell.md), [KNOWLEDGE_GRAPH.md](KNOWLEDGE_GRAPH.md)).
- [ ] **Auth:** pilot role (**Reader** / **Operator** / **Admin**) matches documented policies ([README.md](../../README.md)).
- [ ] **Self-serve trial (automated)** — merge-blocking workflow **`ui-e2e-live`** includes [`live-api-trial-end-to-end.spec.ts`(../../archlucid-ui/e2e/live-api-trial-end-to-end.spec.ts) (register → trial metering → Noop checkout → harness activation → metrics); operator runbook [TRIAL_END_TO_END.md](../runbooks/TRIAL_END_TO_END.md).

---

## 5. Export quality validation

- [ ] **Export** for a committed run (or export record) opens without corruption in the intended viewer (Markdown/DOCX/PDF as you ship) ([ARCHITECTURE_FLOWS.md](ARCHITECTURE_FLOWS.md)).
- [ ] **Replay** of a persisted export record (if used) reproduces expected output or documents known deltas ([ARCHITECTURE_FLOWS.md](ARCHITECTURE_FLOWS.md) Flow B).
- [ ] **Comparison replay** — **artifact** mode returns stored payload; **regenerate** / **verify** behavior understood if pilots rely on drift checks ([ARCHITECTURE_FLOWS.md](ARCHITECTURE_FLOWS.md) Flow C).
- [ ] **ZIP / bundle** download (if exposed): completes; empty-manifest vs missing-manifest error semantics understood ([operator-shell.md](operator-shell.md)).

---

## 6. Naming consistency

- [ ] **User-facing** copy (UI, Swagger titles where customized, CLI operator strings) says **ArchLucid** where product-facing ([V1_SCOPE.md](V1_SCOPE.md) naming note).
- [ ] **Legacy config keys** (`ArchLucid*`, `ARCHIFORGE_*`) documented in runbook; **bridge** behavior verified if both old and new keys appear ([README.md](../../README.md), [GLOSSARY.md](GLOSSARY.md)).
- [ ] **Integration event type strings** — canonical vs legacy aliases understood if consumers exist ([INTEGRATION_EVENTS_AND_WEBHOOKS.md](INTEGRATION_EVENTS_AND_WEBHOOKS.md)).
- [ ] **Image / container names** in deploy docs match what was actually pushed ([CONTAINERIZATION.md](CONTAINERIZATION.md), [RELEASE_LOCAL.md](RELEASE_LOCAL.md)).

---

## 7. Support bundle validation

- [ ] **`dotnet run --project ArchLucid.Cli -- support-bundle --zip`** (against staging/prod-like API) completes without unhandled errors ([CLI_USAGE.md](CLI_USAGE.md), [TROUBLESHOOTING.md](../TROUBLESHOOTING.md)).
- [ ] **Archive reviewed** internally: no unexpected secrets; redaction policy applied before external share ([PILOT_GUIDE.md](PILOT_GUIDE.md#when-you-report-an-issue)).
- [ ] **Key sections present** for triage: e.g. build/health/config summary as documented in [CHANGELOG.md](../CHANGELOG.md) / [TROUBLESHOOTING.md](../TROUBLESHOOTING.md) (manifest, build, health, config-summary, environment, etc.).
- [ ] **Ticket template** for pilots includes: version, correlation ID, bundle (if allowed), repro steps ([PILOT_GUIDE.md](PILOT_GUIDE.md)).

---

## 8. Recovery drill

Pick **at least one** drill appropriate to your tier; record date and outcome.

- [ ] **Quarterly staging chaos calendar** — three scheduled game-day rows + closing-report discipline: [`docs/quality/game-day-log/README.md`](../quality/game-day-log/README.md) (Simmy workflow cron aligns; production chaos stays item **34** in [`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md)).
- [ ] **Process / instance** — restart API (and Worker if deployed); verify **`/health/ready`** and one **request → commit** smoke.
- [ ] **SQL connectivity** — controlled disconnect/reconnect or failover exercise per [runbooks/DATABASE_FAILOVER.md](../runbooks/DATABASE_FAILOVER.md) (or equivalent for your host).
- [ ] **Rollback plan** — previous API version + DB migration direction documented (forward-only migrations: note restore-from-backup strategy) ([SQL_SCRIPTS.md](SQL_SCRIPTS.md)).
- [ ] **RTO/RPO** — actual observed times vs [RTO_RPO_TARGETS.md](RTO_RPO_TARGETS.md) noted for this environment tier.

---

## 9. Known issues and deferrals

- [ ] **Known issues** list attached to release (internal or pilot-facing): symptom, workaround, target fix.
- [ ] **Deferred work** references [V1_SCOPE.md](V1_SCOPE.md) §3 or your backlog IDs (no implied warranty for deferred areas).
- [ ] **Playwright / UI E2E** — if only mock-backed E2E ran, state clearly that **live API+UI** was validated manually ([RELEASE_SMOKE.md](RELEASE_SMOKE.md)).
- [ ] **Security review** items (Entra, keys, private endpoints) closed or explicitly waived with owner ([GOLDEN_PATH.md](GOLDEN_PATH.md), [onboarding/day-one-security.md](../onboarding/day-one-security.md)).

---

## Sign-off (optional)

| Role | Name | Date | Notes |
|------|------|------|-------|
| Release owner | | | |
| SRE / platform | | | |
| Security (if required) | | | |

---

**Maintenance:** When release practice changes, update this checklist and cross-links in [V1_SCOPE.md](V1_SCOPE.md) §6 and [ARCHITECTURE_INDEX.md](../ARCHITECTURE_INDEX.md).
