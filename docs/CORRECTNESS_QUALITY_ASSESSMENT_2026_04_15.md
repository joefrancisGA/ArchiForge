# ArchLucid Solution Correctness Quality Assessment — 2026-04-15

**Assessor:** Automated deep-dive analysis of repository code, tests, CI/CD, docs, and infrastructure.

**Methodology:** Evidence-based assessment across 12 weighted correctness dimensions. Each dimension scored 1–100, weighted by impact on production correctness. Areas are ordered from **most improvement needed** to **least improvement needed** (by weighted gap). Final composite score at the end.

---

## Overall Score: 66 / 100

**Verdict:** The solution demonstrates strong *architectural ambition* and an unusually thorough documentation posture for a pre-revenue product, but significant correctness gaps remain in test coverage depth, concurrency guarantees, data lifecycle completeness, and production hardening. The foundation is solid enough for a controlled pilot; it is not yet at the level where a customer could operate it with confidence absent deep vendor support.

---

## Scoring Legend

| Range | Meaning |
|-------|---------|
| 90–100 | Excellent — minimal risk, production-hardened |
| 75–89 | Strong — targeted gaps only |
| 60–74 | Adequate — functional but material correctness risks |
| 45–59 | Weak — significant correctness concerns |
| Below 45 | Critical — blocking or high-risk |

---

## Dimension Assessments (most improvement needed first)

### 1. Branch and Mutation Coverage Depth — Score: 52 / 100 (Weight: 10)

**Weighted Gap: 480**

**Evidence:**
- Merged line coverage is 77.82%, branch coverage is 63.38%. The stated aspiration is 100%.
- Stryker mutation score baseline is 65% with a target ratchet to 72%. This means **35% of mutations survive** — over one-third of logical decision paths lack assertions that would catch a simple inversion or boundary shift.
- The lowest assembly (`ArchLucid.Persistence`) sits at **53% line coverage** with 6,053 coverable lines. The three worst files (`GoldenManifestPhase1RelationalRead`, `GraphSnapshotRelationalRead`, `FindingsSnapshotRelationalRead`) collectively have **641 uncovered lines** — these are the core relational data readers that reconstruct domain aggregates from SQL.
- `ArchLucid.Api` at 64.54% with **13,662 coverable lines** means roughly **4,850 lines** of API controller/service logic lack any test assertion.
- Branch coverage at 63% means about **1 in 3 conditional branches** has never been exercised by tests. For an architecture decision tool that produces compliance findings, this is a meaningful correctness risk — edge-case branches in finding engines, governance workflows, and manifest merging could silently produce wrong results.

**Tradeoffs:** Higher Stryker coverage is expensive in CI minutes (already scheduled, not per-PR). FsCheck property tests exist but cover only a narrow slice of the domain.

**Justification for score:** Line coverage alone would suggest ~70, but branch coverage at 63% and mutation survival at 35% indicate that the *quality* of existing tests is moderate — many tests validate happy paths without probing boundaries or error branches. The gap between "lines touched" and "mutations killed" is the most telling signal.

---

### 2. Concurrency and Race Condition Correctness — Score: 55 / 100 (Weight: 9)

**Weighted Gap: 405**

**Evidence:**
- `Data Consistency Matrix` explicitly acknowledges: "Idempotency on run creation is best-effort under extreme duplicate-key races."
- Governance approval uses `SERIALIZABLE` + `UPDLOCK, ROWLOCK` with a parallel contract test — this is the *only* concurrency-critical path with a dedicated race-condition test.
- `ROWVERSION`-based optimistic concurrency on `dbo.Runs` exists but there is no test that exercises two parallel writers hitting the same run and validating the 409 response.
- Outbox processing has no documented exactly-once guarantee — the consumer contract states "at-least-once" which is correct, but no idempotency test exists for the consumer side.
- 57 controllers produce mutation side effects (create/update/delete runs, governance decisions, policy packs, alerts, archival). Only the governance approval path has a concurrency test.
- Hot-path cache invalidation relies on application-level write-through — any write path that bypasses the repository (ad-hoc SQL, future migration scripts, data fixes) will serve stale data until TTL expires. No test validates this boundary.
- Background job queue (`InMemoryBackgroundJobQueue`) uses `ConcurrentQueue` — correct for single-instance but no test exercises multi-thread dequeue contention.

**Tradeoffs:** Full concurrency testing requires database-level test infrastructure (multiple connections, controlled timing). The `SqlServerContainer` pattern exists in tests but is used sparingly.

**Justification for score:** The governance concurrency test shows awareness of the problem. But 56 of 57 mutation surfaces lack any concurrency assertion. For a system that handles architecture compliance decisions with audit trails, concurrent correctness is not optional.

---

### 3. Data Lifecycle and Cascade Completeness — Score: 58 / 100 (Weight: 8)

**Weighted Gap: 336**

**Evidence:**
- Run archival cascade is *explicitly incomplete*: "downstream rows may remain until a dedicated cleanup job or future migration." Child `FindingsSnapshots`, `GraphSnapshots`, `GoldenManifests`, `ArtifactBundles`, `ComparisonRecords`, and `AgentExecutionTraces` may orphan when a parent `Run` is archived.
- Orphan detection exists (`DataConsistencyOrphanProbeHostedService`) but remediation is detection-only with manual runbook resolution.
- `ArchivedUtc` column was added to manifests and findings snapshots (migration 066), but the cascade from `Runs.ArchivedUtc` to child tables relies on application code, not database constraints.
- No `ON DELETE CASCADE` or `ON DELETE SET NULL` foreign key constraints in the DDL — all referential integrity is application-enforced.
- Audit events have no archival coordinator integration — `dbo.AuditEvents` grows unbounded until manual operator intervention.
- Agent execution trace blobs in Azure Blob Storage have no lifecycle policy defined — traces accumulate indefinitely.
- 92 SQL migration files with forward-only DbUp — rollback scripts exist for migrations 056–065 only (10 of 66 total). A bad migration in production requires manual intervention for 56 migrations.

**Tradeoffs:** Database-level cascades risk unintended mass deletes. Application-enforced cascades allow selective archival but require completeness testing that doesn't exist.

**Justification for score:** The detection infrastructure is thoughtful, but detection without remediation in a system that creates compliance artifacts is a significant correctness gap. Data that should be gone but lingers creates audit and compliance confusion.

---

### 4. Error Path and Boundary Validation — Score: 60 / 100 (Weight: 8)

**Weighted Gap: 320**

**Evidence:**
- RFC 9457 Problem Details is applied to `AdminController`, `JobsController`, `DemoController` and the global exception filter — but a broader controller sweep is documented as incomplete ("rfc9457-controller-sweep" prompt exists, meaning bare `NotFound()` / `Conflict()` responses remain on some of the 57 controllers).
- `ArchLucid.Api` has 13,662 coverable lines at 64.54% coverage — the uncovered portion disproportionately represents error paths (controllers tend to have well-tested happy paths, with error/validation/edge branches untested).
- `ApiControllerProblemDetailsSourceGuardTests` checks that no bare `NotFound()` exists in Controllers — but this is a static analysis test, not a behavioral test that validates correct error payloads are returned for specific failure scenarios.
- `FluentValidation` is used for request validation, but the `AdditionalFluentValidationTests` file (7 facts) covers only a subset of the ~30+ endpoints that accept complex request bodies.
- Agent handler error paths: `ComplianceAgentHandler` and `CriticAgentHandler` each have 55 uncovered lines — these represent error handling and fallback paths in the LLM integration layer.
- `DecisionEngineService` error paths during manifest merging have only 2 test facts — merge conflicts, schema validation failures, and partial-result scenarios are lightly covered.

**Tradeoffs:** Error path testing is tedious but high-value. The existing `ProblemDetails` guard prevents the worst class of error (unstructured error responses) but doesn't validate correctness of error content.

**Justification for score:** Happy paths work. Error paths are partially covered. For an architecture compliance tool, the difference between a correct error ("this manifest has a schema version mismatch") and a generic 500 is the difference between operator self-service and a support ticket.

---

### 5. Domain Invariant Enforcement — Score: 62 / 100 (Weight: 7)

**Weighted Gap: 266**

**Evidence:**
- Governance state machine: FsCheck property tests exist for segregation-of-duties (self-approval), terminal state rejection, and promotion manifest-version matching. But the state machine has more transitions than are tested — for example, transition from `Draft` → `Submitted` → `Approved` → `Promoted` is tested, but `Draft` → `Submitted` → `Rejected` → (re-submit?) is not.
- `ManifestVersionIncrementRules` extraction with edge tests — good isolation.
- Alert deduplication: `CompositeAlertDeduplicationKeyBuilderPropertyTests` (FsCheck) exist — but the dedup key space is complex (multi-field composite) and the property tests generate random keys without testing real-world collision scenarios.
- Run lifecycle state machine: `RunLifecycleStatePropertyTests` covers `CommitRunAsync` invariants for `ReadyForCommit`, `Failed`, `Created`. But the full lifecycle (`Created` → `Executing` → `WaitingForResults` → `ReadyForCommit` → `Committed`) is not property-tested end-to-end.
- Finding engine correctness: 9+ finding engine types exist. Coverage varies widely — `PolicyCoverageFindingEngine` has 4 facts, `SecurityBaselineFindingEngine` has 5, but `ComplianceFindingEngine` (which produces the compliance findings that governance workflows act on) test count is not visible in the top-level search.
- No invariant test for: "a committed run must have exactly 4 agent results (Topology, Cost, Compliance, Critic) before manifest generation."

**Tradeoffs:** Property-based testing is powerful but requires careful generator design. The FsCheck infrastructure is in place; the gap is coverage breadth.

**Justification for score:** Core invariants are partially tested. The governance workflow (the most safety-critical domain) has the best coverage. But the finding engines — which produce the data that governance acts on — have thinner invariant testing.

---

### 6. Integration Boundary Correctness — Score: 65 / 100 (Weight: 7)

**Weighted Gap: 245**

**Evidence:**
- OpenAPI contract snapshot testing (drift detection in CI) — excellent practice that prevents accidental API contract breaks.
- NSwag-generated client (`ArchLucidApiClient.g.cs`) with roundtrip tests.
- Integration event payload contract tests assert payloads against committed JSON schemas — good for event-driven boundaries.
- `ArchLucid.Architecture.Tests` with NetArchTest for layering — 17 dependency constraint facts.
- Live E2E tests (15+ `live-api-*.spec.ts` specs) against real API + SQL — covers happy path, conflict, governance rejection, negative paths, concurrency, archival.
- **Gaps:** No Pact-style consumer-driven contract tests. No contract test for the CLI → API boundary beyond `ArchLucidCliApiClientHttpTests` (8 facts). No contract test for webhook payload consumers. The 6 integration event types have schema tests but no consumer-side deserialization test. No contract for the UI proxy → API boundary (the proxy does request transformation).

**Tradeoffs:** Pact adds infrastructure complexity. The OpenAPI snapshot test provides most of the value for REST boundaries. Event schemas fill the async gap partially.

**Justification for score:** REST contract testing is strong. Async/event and CLI boundaries are adequate. No consumer-driven contract testing means changes that satisfy the provider but break consumers can slip through.

---

### 7. Observability Correctness — Score: 72 / 100 (Weight: 5)

**Weighted Gap: 140**

**Evidence:**
- 30+ custom OTel metrics with clear naming conventions (`archlucid_*`). 8 custom activity sources.
- Business KPI metrics: runs created, findings by severity, LLM calls per run, quality scores.
- Prometheus recording rules and SLO rules in Terraform with committed alert configs.
- `LogSanitizer` applied systematically to user-input-derived log parameters (CodeQL-driven).
- Grafana dashboards committed (authority, SLO, LLM usage, run lifecycle).
- **Gaps:** Some metrics use `Counter.Add` with `KeyValuePair<string, object?>` tags — this is correct but mixing tag construction styles across the codebase (some use `TagList`, some use `KeyValuePair`) creates maintenance risk. No test validates that all emitted metric names match the Prometheus alert rules (a renamed metric would silently break alerting). Sampling ratio configuration is documented but not tested.

**Justification for score:** The observability infrastructure is mature and well-structured. The gap is in validating the correctness of the observability pipeline itself — can you trust that every metric name in code matches every metric reference in Prometheus rules?

---

### 8. Security Configuration Correctness — Score: 70 / 100 (Weight: 6)

**Weighted Gap: 180**

**Evidence:**
- `AuthSafetyGuard.GuardAllDevelopmentBypasses` prevents dev bypass in production — critical guard with tests.
- RBAC with 4 roles (Reader, Operator, Admin, Auditor) and per-controller `[Authorize]` policies.
- SQL RLS with `SESSION_CONTEXT` — residual risk documented and accepted.
- OWASP ZAP in CI, Schemathesis per-PR (`--phases=examples`), CodeQL, Gitleaks, Trivy.
- API key rotation with comma-separated overlap.
- Content safety guard (optional, stub-throws-when-enabled-but-unconfigured — good fail-safe).
- Rate limiting with role-aware partition keys.
- **Gaps:** `DevelopmentBypass` mode still exists as a code path — even with the guard, it's a single `if` check away from being bypassed by a misconfigured environment variable. The guard checks `IHostEnvironment.IsProduction()` and `ARCHLUCID_ENVIRONMENT` but if neither signals production (e.g., an environment named "staging-prod"), the bypass would be allowed. No `[Authorize]` audit test that verifies *every* mutation endpoint has an explicit policy (only spot-checks). `Authentication:ApiKey:DevelopmentBypassAll` is a configuration key that if set to `true` in a non-production environment opens all endpoints — no per-endpoint bypass granularity.

**Justification for score:** The security posture is solid for a V1 product. The DevelopmentBypass guard is the right approach. The gaps are in edge-case configuration scenarios and comprehensive authorization coverage testing.

---

### 9. Build and CI Pipeline Correctness — Score: 75 / 100 (Weight: 5)

**Weighted Gap: 125**

**Evidence:**
- Multi-tier CI: fast core → greenfield SQL boot → full regression → k6 smoke → Schemathesis → ZAP → UI E2E (mock + live).
- Coverage gate at 71% merged line (CI-enforced via `assert_merged_line_coverage_min.py`).
- Stryker mutation testing (scheduled, not per-PR) with baseline ratchet.
- OpenAPI snapshot test prevents API contract drift.
- Architecture constraint tests prevent dependency violations.
- CI rename guard prevents accidental reintroduction of `ArchiForge` strings.
- `assert_rollback_scripts_exist.py` for migration rollback coverage.
- `assert_prompt_regression.py` for agent prompt shape checks.
- **Gaps:** Stryker is scheduled-only (not per-PR) — mutation regressions can merge before the next scheduled run catches them. k6 soak test is `continue-on-error`. Live E2E JWT is `continue-on-error`. The CI pipeline has ~15 jobs but no dependency DAG visualization — it's unclear if all paths are correctly ordered. No CI job validates that Terraform `plan` succeeds (only docs reference Terraform).

**Justification for score:** The CI pipeline is comprehensive by industry standards. The gap is in making all quality gates merge-blocking rather than informational.

---

### 10. Configuration and Startup Correctness — Score: 73 / 100 (Weight: 4)

**Weighted Gap: 108**

**Evidence:**
- `ArchLucidConfigurationRules.CollectErrors` validates configuration at startup with fail-fast.
- `ArchLucidLegacyConfigurationWarnings.LogIfLegacyKeysPresent` warns on deprecated keys.
- Legacy configuration sunset constant (`2027-07-01`) with doc alignment.
- `appsettings.Advanced.json` for optional tuning — loaded after defaults with `reloadOnChange: true`.
- `IOptionsMonitor` for runtime-reloadable circuit breaker and rate limit config.
- **Gaps:** No test validates that `appsettings.json` + `appsettings.Development.json` + environment variables compose correctly for every documented scenario (DevelopmentBypass, JwtBearer, ApiKey, Sql storage, InMemory storage). The configuration rules are a positive list (known errors) — unknown invalid combinations may pass silently. `reloadOnChange: true` on `appsettings.Advanced.json` means a bad edit to this file on a running system could cause partial configuration failures without restart.

**Justification for score:** Startup validation is above average. The gap is in exhaustive configuration combination testing.

---

### 11. Documentation-to-Code Alignment — Score: 74 / 100 (Weight: 3)

**Weighted Gap: 78**

**Evidence:**
- 243+ markdown files — extensive and well-structured with architecture index, glossary, onboarding, runbooks.
- CI guards for specific doc assertions (audit count anchors, rename allowlists, traceability matrix).
- `FIRST_5_DOCS.md`, `START_HERE.md`, `CODE_MAP.md` for navigation.
- ADRs with numbered decisions.
- **Gaps:** No automated broken-link checker for internal markdown references. Some docs reference the prior weighted assessment as canonical but the checklist log references ongoing changes — temporal alignment between docs and code is manually maintained. 193+ docs risk staleness — no "last reviewed" dates on operational runbooks. Multiple overlapping docs on the same topic (3 quality assessments, 2 test structure docs, multiple cursor prompt docs).

**Justification for score:** Documentation quantity is excellent. Documentation accuracy relative to rapidly evolving code is a managed risk, not a verified guarantee.

---

### 12. Schema and Data Migration Correctness — Score: 71 / 100 (Weight: 4)

**Weighted Gap: 116**

**Evidence:**
- DbUp with 66+ migrations and a master DDL script (`ArchLucid.sql`).
- `GreenfieldSqlBootIntegrationTests` validates that DbUp runs cleanly on an empty catalog.
- `TenantScopedTableDdlTests` validates that `dbo.Runs` has tenant scoping columns.
- Migration rollback scripts exist for 10 of 66 migrations (migrations 056–065).
- Persistence contract tests validate schema expectations for repositories.
- **Gaps:** 56 of 66 migrations have no rollback script. The master DDL and incremental migrations could theoretically diverge — no test validates that running all migrations produces the same schema as the master DDL. No migration idempotency test (running the same migration twice). Historical migrations are explicitly excluded from modification — if a bug exists in migration 015, it must be corrected by a new forward migration, adding schema complexity over time.

**Justification for score:** The greenfield boot test is a strong correctness gate. The rollback gap is the primary concern — in production, a bad migration with no rollback script requires manual DBA intervention.

---

## Summary Table (ordered by weighted gap, descending — most improvement needed first)

| Rank | Dimension | Weight | Score | Gap | Weighted Gap | Grade |
|------|-----------|--------|-------|-----|--------------|-------|
| 1 | **Branch/Mutation Coverage Depth** | 10 | 52 | 48 | **480** | Weak |
| 2 | **Concurrency/Race Correctness** | 9 | 55 | 45 | **405** | Weak |
| 3 | **Data Lifecycle/Cascade** | 8 | 58 | 42 | **336** | Weak |
| 4 | **Error Path/Boundary Validation** | 8 | 60 | 40 | **320** | Adequate |
| 5 | **Domain Invariant Enforcement** | 7 | 62 | 38 | **266** | Adequate |
| 6 | **Integration Boundary Correctness** | 7 | 65 | 35 | **245** | Adequate |
| 7 | **Security Configuration** | 6 | 70 | 30 | **180** | Adequate |
| 8 | **Observability Correctness** | 5 | 72 | 28 | **140** | Adequate |
| 9 | **Build/CI Pipeline** | 5 | 75 | 25 | **125** | Strong |
| 10 | **Schema/Migration Correctness** | 4 | 71 | 29 | **116** | Adequate |
| 11 | **Configuration/Startup** | 4 | 73 | 27 | **108** | Adequate |
| 12 | **Documentation-to-Code Alignment** | 3 | 74 | 26 | **78** | Adequate |

**Composite weighted score:** 4,921 / 7,600 = **64.8%**

**Unweighted average:** 65.6 / 100

---

## Composite Score Reconciliation

| Metric | Value |
|--------|-------|
| **This assessment (correctness focus)** | **66 / 100** |
| Prior weighted assessment (broader scope, 2026-04-14) | 68.5 / 100 |
| SaaS marketability assessment | 46.1 / 100 |

The lower correctness score relative to the broader assessment is expected: correctness is a stricter lens. The broader assessment includes dimensions where the solution scores well (observability 82, documentation 80, deployability 76) that dilute the correctness gaps.

---

## Six Best Improvements (ordered by weighted impact on correctness)

### Improvement 1: Targeted Branch Coverage Assault on Persistence and API (Weighted Gap: 480)

**What:** Raise branch coverage from 63% to 75% by writing tests specifically targeting uncovered conditional branches in the three lowest assemblies (`ArchLucid.Persistence` 53%, `ArchLucid.Api` 64.5%, `ArchLucid.Application` 75.5%).

**Why this is #1:** Weight 10, gap 48 — this is the single highest-leverage investment. Every uncovered branch in Persistence (the relational readers that reconstruct domain aggregates) represents a path where data could be silently malformed without any test catching it.

**Approach:** For each of the 3 worst files per assembly (9 files total per the coverage analysis), read the source, identify untested `if`/`switch`/`??` branches, and write targeted branch-coverage tests with `FluentAssertions`. Prioritize null-handling branches and error-return paths.

**Cursor prompt:**

```
Branch coverage assault — Persistence relational readers

Goal: raise branch coverage in ArchLucid.Persistence from 53% toward 65%.
Focus on the three files with the most uncovered lines:
  1. GoldenManifestPhase1RelationalRead.cs (258 uncovered)
  2. GraphSnapshotRelationalRead.cs (198 uncovered)
  3. FindingsSnapshotRelationalRead.cs (185 uncovered)

For each file:
1. Read the source and identify every if/switch/??/ternary branch.
2. List which branches are likely untested (null-coalescing fallbacks,
   empty-collection guards, JSON-vs-relational path switches).
3. Write SQL Server integration tests (using existing SqlServerContainer
   pattern in ArchLucid.Persistence.Tests) that exercise each uncovered branch.
   For each branch, set up SQL data that forces the alternate path, then assert
   the output matches expectations.
4. Add [Trait("Suite", "Core")] and [Trait("Category", "Integration")]
   on each test class.
5. Each test class in its own file. Use concrete types, not var.
   Do not use ConfigureAwait(false).
6. Run: dotnet test ArchLucid.Persistence.Tests -c Release
   --filter "Suite=Core&Category=Integration"

Target: at least 15 new branch-coverage tests across the three files.
```

---

### Improvement 2: Concurrency Correctness Tests for Top 5 Mutation Endpoints (Weighted Gap: 405)

**What:** Add parallel-execution tests for the 5 most safety-critical mutation endpoints: run creation, run commit, governance approval, run archival, and policy pack publish.

**Why this is #2:** Weight 9, gap 45. Only governance approval has a concurrency test today. A race condition in run commit could produce duplicate manifests; a race in archival could partially cascade.

**Approach:** Use `Task.WhenAll` with 8–32 parallel calls against a shared `SqlServerContainer` database. Assert exactly-once outcomes (one winner, N-1 losers with 409/conflict).

**Cursor prompt:**

```
Concurrency correctness — top 5 mutation endpoints

Goal: prove exactly-once semantics for the 5 most critical mutation paths.

For each of the following operations, create a new test class in the
appropriate *.Tests project with [Trait("Suite", "Core")] and
[Trait("Category", "Integration")]:

1. Run creation (POST /v1/architecture/request) — 16 parallel calls with
   the same idempotency key must produce exactly 1 run. Assert the run count
   in SQL is 1 and all responses return the same runId.

2. Run commit (POST /v1/architecture/run/{runId}/commit) — 8 parallel
   commits for the same run must produce exactly 1 manifest. Assert
   manifest count is 1 and 7 responses are 409 Conflict.

3. Governance approval — already has a test. Extend it: 32 parallel
   approve calls. Assert exactly 1 succeeds and 31 get conflict/invalid.

4. Run archival (POST /v1/admin/runs/archive-by-ids) — 4 parallel
   archive calls for the same run. Assert ArchivedUtc is set exactly once
   and child snapshots are cascaded (or orphan probe detects none).

5. Policy pack publish — 8 parallel publish calls for the same draft
   version. Assert exactly 1 published version exists.

Use SqlServerContainer for real SQL. Use WebApplicationFactory for API
tests. Each test class in its own file. Do not use ConfigureAwait(false).

Run: dotnet test [project] -c Release --filter "FullyQualifiedName~Concurrency"
```

---

### Improvement 3: Complete Run Archival Cascade with Correctness Tests (Weighted Gap: 336)

**What:** Implement transactional cascade from `Runs.ArchivedUtc` to all child tables (`FindingsSnapshots`, `GraphSnapshots`, `GoldenManifests`, `ArtifactBundles`, `ComparisonRecords`, `AgentExecutionTraces`) and add integration tests proving no orphans remain.

**Why this is #3:** Weight 8, gap 42. The current state (detection without remediation) means archived runs leave orphaned child data that accumulates indefinitely, consuming storage and creating audit confusion.

**Cursor prompt:**

```
Run archival cascade completeness

Goal: when a run is archived, all child aggregate rows must be cascade-archived
in the same transaction. No orphans.

1. Read ArchLucid.Persistence/Runs/SqlRunRepository.cs — find the archive
   method (ArchiveRunsByIds or ArchiveRunsCreatedBeforeAsync).

2. Within the same SQL transaction that sets Runs.ArchivedUtc:
   a. UPDATE dbo.FindingsSnapshots SET ArchivedUtc = @now WHERE RunId IN (...)
   b. UPDATE dbo.GraphSnapshots SET ArchivedUtc = @now WHERE RunId IN (...)
   c. UPDATE dbo.GoldenManifests SET ArchivedUtc = @now WHERE RunId IN (...)
   d. UPDATE dbo.ArtifactBundles SET ArchivedUtc = @now WHERE RunId IN (...)
   e. UPDATE dbo.ComparisonRecords SET ArchivedUtc = @now
      WHERE LeftRunId IN (...) OR RightRunId IN (...)
   f. UPDATE dbo.AgentExecutionTraces SET ArchivedUtc = @now WHERE RunId IN (...)

   If any child table lacks an ArchivedUtc column, add a forward migration.

3. Update ArchLucid.sql master DDL to reflect any new columns.

4. Write SQL integration tests (SqlServerContainer) that:
   a. Insert a run + all child types.
   b. Archive the run.
   c. Assert ArchivedUtc is set on every child row.
   d. Assert the orphan probe returns zero orphans for that run.

5. Update docs/DATA_CONSISTENCY_MATRIX.md to mark cascade as transactional.

6. Run: dotnet test ArchLucid.Persistence.Tests -c Release
   --filter "FullyQualifiedName~ArchivalCascade"
   && dotnet build ArchLucid.sln -c Release
```

---

### Improvement 4: RFC 9457 Problem Details Sweep Across All Controllers (Weighted Gap: 320)

**What:** Audit every controller action for bare `NotFound()`, `Conflict()`, `BadRequest()` returns and replace them with structured `Problem()` responses including `correlationId`, `detail`, and `instance`.

**Why this is #4:** Weight 8, gap 40. Inconsistent error formats mean API consumers cannot reliably parse errors — they must handle both structured and unstructured responses.

**Cursor prompt:**

```
RFC 9457 Problem Details — full controller sweep

Goal: every non-2xx response from every controller must return
application/problem+json with correlationId, detail, and instance.

1. Read ArchLucid.Api/Controllers/ — all 57 files.

2. Search for patterns that return bare ActionResult without Problem Details:
   - return NotFound();
   - return NotFound("...");
   - return Conflict();
   - return Conflict(new { ... });
   - return BadRequest();
   - return BadRequest("...");
   - return StatusCode(nnn);
   - return new ObjectResult(...) { StatusCode = nnn };

3. For each occurrence, replace with a call through the existing
   ApplicationProblemMapper or the Problem() helper that attaches
   ProblemCorrelation (see ApiProblemDetailsExceptionFilter for the pattern).

4. Update or add a test in ArchLucid.Api.Tests that validates the
   response Content-Type is application/problem+json and contains
   a correlationId field.

5. Extend ApiControllerProblemDetailsSourceGuardTests to scan for all
   bare error return patterns (not just NotFound).

6. Run: dotnet test ArchLucid.Api.Tests -c Release
   --filter "FullyQualifiedName~ProblemDetails"
   && dotnet build ArchLucid.sln -c Release
```

---

### Improvement 5: FsCheck Property Tests for Full Run Lifecycle State Machine (Weighted Gap: 266)

**What:** Create a comprehensive FsCheck property test suite that models the complete run lifecycle state machine (`Created` → `Executing` → `WaitingForResults` → `ReadyForCommit` → `Committed` or `Failed` or `Archived`) and validates all transition invariants.

**Why this is #5:** Weight 7, gap 38. The run lifecycle is the central domain concept. Individual transition tests exist, but no property test validates the full state machine as a composition of transitions.

**Cursor prompt:**

```
FsCheck run lifecycle state machine property tests

Goal: prove that all valid run lifecycle transitions produce correct states
and all invalid transitions are rejected.

1. Define an FsCheck generator that produces random sequences of lifecycle
   commands: Create, Execute, WaitForResults, Commit, Fail, Archive.

2. Model the expected state machine:
   - Created → Executing (Execute command)
   - Executing → WaitingForResults (all 4 agents complete)
   - WaitingForResults → ReadyForCommit (evaluation passes)
   - ReadyForCommit → Committed (Commit command)
   - Any non-terminal → Failed (unrecoverable error)
   - Committed → Archived (Archive command)
   - Failed → Archived (Archive command)

3. For each random command sequence, track expected state and compare
   against actual behavior by calling the orchestrators through
   NSubstitute-mocked repositories.

4. Property invariants to assert:
   a. A committed run always has exactly 1 golden manifest.
   b. A failed run never has a golden manifest.
   c. An archived run has ArchivedUtc set.
   d. No transition can move from a terminal state to a non-terminal state.
   e. Commit requires all 4 agent results.
   f. Double-commit returns 409, not a second manifest.

5. Place in ArchLucid.Application.Tests/Runs/ with [Trait("Suite", "Core")].
   Each test class in its own file. Do not use ConfigureAwait(false).

6. Run: dotnet test ArchLucid.Application.Tests -c Release
   --filter "FullyQualifiedName~RunLifecycleStateMachine"
```

---

### Improvement 6: Migration Rollback Script Coverage for Migrations 001–055 (Weighted Gap: 116 from Schema + cascading benefit to Reliability)

**What:** Generate rollback scripts for the 56 migrations that currently lack them, prioritizing the most recent 20 (migrations 036–055) which are most likely to need rollback in active development.

**Why this is #6:** Weight 4 for schema + indirect reliability benefit. In production, a bad migration without a rollback script requires a DBA to write emergency SQL under pressure — a significant operational risk for a compliance-oriented product.

**Cursor prompt:**

```
Migration rollback scripts — backfill 036–055

Goal: every migration from 036 onward has a corresponding rollback script
in ArchLucid.Persistence/Migrations/Rollback/.

1. Read each migration file 036_*.sql through 055_*.sql.

2. For each, create a rollback script R036_*.sql through R055_*.sql that
   reverses the forward migration:
   - CREATE TABLE → DROP TABLE IF EXISTS
   - ALTER TABLE ADD COLUMN → ALTER TABLE DROP COLUMN
   - CREATE INDEX → DROP INDEX IF EXISTS
   - INSERT (seed data) → DELETE WHERE (matching key)
   - UPDATE → reverse UPDATE (document the original value in a comment)
   - For complex migrations, add a header comment explaining manual steps.

3. Each rollback script should be idempotent (safe to run twice).

4. Update scripts/ci/assert_rollback_scripts_exist.py to validate
   that every migration ≥ 036 has a corresponding rollback script.

5. Update docs/DATABASE_MIGRATION_ROLLBACK.md with the expanded coverage.

6. Run: python scripts/ci/assert_rollback_scripts_exist.py
   && dotnet build ArchLucid.sln -c Release
```

---

## Strategic Observations

### What the solution does well
- **Architectural discipline:** NetArchTest, assembly-reference tests, and DI registration tests create a structural correctness envelope that most projects this size lack.
- **Observability-first:** 30+ custom metrics with Prometheus rules committed alongside code — observability is not an afterthought.
- **Documentation volume:** 243+ docs with architecture index, onboarding paths, and runbooks — unusual depth for a pre-revenue product.
- **Security layering:** ZAP + Schemathesis + CodeQL + Gitleaks + Trivy + RLS + DevelopmentBypass guard — defense in depth is genuine.
- **Honest self-assessment:** The `V1_READINESS_SUMMARY.md` is refreshingly candid about what works and what doesn't.

### What creates the most correctness risk
- **Branch coverage gap:** 63% branch coverage in a compliance tool means 1-in-3 decision branches has never been tested.
- **Application-enforced cascades without tests:** Every cascade path is a potential orphan source, and orphans in a compliance audit system undermine trust.
- **Single concurrency test:** 57 mutation endpoints, 1 concurrency test — the probability of an undiscovered race condition is high.
- **Forward-only migrations without rollback:** 56 of 66 migrations have no escape hatch. A production incident requiring rollback would be a crisis.

### The rename tax
The ArchiForge → ArchLucid rename has consumed significant engineering attention (the progress log in `ARCHLUCID_RENAME_CHECKLIST.md` documents ~60 sessions). While the rename is substantially complete in code, the deferred items (Terraform state mv, GitHub repo, Entra apps, workspace path) create a persistent cognitive tax and an ongoing source of merge noise. The rename was necessary but it has displaced correctness work.
