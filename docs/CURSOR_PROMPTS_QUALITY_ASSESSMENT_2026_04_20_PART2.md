> **Scope:** Cursor prompts — Quality Assessment 2026-04-20 (Improvements 4–6) - full detail, tables, and links in the sections below.

# Cursor prompts — Quality Assessment 2026-04-20 (Improvements 4–6)

These are paste-ready Agent prompts for **improvements 4, 5, and 6** identified in **[`QUALITY_ASSESSMENT_2026_04_20_WEIGHTED_80_72.md`](QUALITY_ASSESSMENT_2026_04_20_WEIGHTED_80_72.md)** § 3. Improvements 1 and 2 live in **[`CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_20.md`](CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_20.md)**.

Each prompt is **self-contained**, names the canonical files / seams a contributor must touch, lists existing CI gates that must stay green, and ends with explicit acceptance criteria. They follow the workspace conventions in `.cursor/rules/` (early-return, `is null`, primary constructors, single-line guards, LINQ pipelines, single class per file), the **Do-The-Work-Yourself** rule (no subagents), the **Markdown-Generosity** rule (each prompt produces a user-facing Markdown artifact alongside the code), the **Security-Default-Rule-Port-445-Alignment** rule (no public SMB), and the SQL convention "All SQL DDL should be in a single file for each database" (consolidated in **`ArchLucid.Persistence/Scripts/ArchLucid.sql`**).

---

## Prompt 3 — Lift `ArchLucid.Api` correctness gates and ship Marketplace `ChangePlan` / `ChangeQuantity` to GA

> Numbered "Prompt 3" in this file because Prompts 1 and 2 ship in the companion document. This is **Improvement 4** in the assessment.

**Quality lift:** Correctness (80 → ~86) by closing the documented `ArchLucid.Api` per-package coverage shortfall (CI floor is **≥ 79 % line / ≥ 63 % branch / ≥ 63 % per-package**, and `docs/CODE_COVERAGE.md` calls out `ArchLucid.Api` at "**~60 %**"); Testability (85 → ~89) by extending Stryker mutation testing to `ArchLucid.Api`; Marketability (78 → ~80) by removing the `Billing:AzureMarketplace:GaEnabled=false` `AcknowledgedNoOp` short-circuit on `ChangePlan` / `ChangeQuantity` so the transactability story is real, not aspirational.

### Paste this into Cursor Agent

> **Goal.** Land **three concurrent correctness lifts** in one focused PR:
>
> 1. **Cover `ArchLucid.Api` to ≥ 79 % per-package line** so the strict-profile gate in **`.github/workflows/ci.yml`** stops failing for that package (no `--skip-package-line-gate`, no per-package waiver).
> 2. **Extend Stryker** to `ArchLucid.Api` with a new **`stryker-config.api.json`** matching the shape of the existing `stryker-config.application.json` (project + test-projects pair, html/json/progress reporters, threshold floor honest with the reality of HTTP wiring code — start at `break: 55`, ratchet later via `scripts/ci/refresh_stryker_baselines.py`).
> 3. **Promote Marketplace `ChangePlan` / `ChangeQuantity` to GA** by defaulting **`Billing:AzureMarketplace:GaEnabled=true`** in `ArchLucid.Api/appsettings.json`, removing the `AcknowledgedNoOp` short-circuit, and replacing it with the existing `sp_Billing_ChangePlan` / `sp_Billing_ChangeQuantity` paths under a documented rollback runbook.
>
> **Non-goals.** Do not weaken the merged-line / merged-branch gates in `.github/workflows/ci.yml`. Do not add `[ExcludeFromCodeCoverage]` to controllers or middleware to inflate the percentage — coverage must come from real tests. Do not modify any historical `00x` / `0xx` migration. Do not change Stripe paths. Do not introduce new public endpoints.
>
> **Steps (do them yourself; do not delegate to subagents):**
>
> 1. **Read first:**
>    - **`docs/CODE_COVERAGE.md`** (strict profile, current per-package shortfall on `ArchLucid.Api`, late-session controller/middleware test list).
>    - **`docs/coverage-exclusions.md`** (allowed exclusion categories — Stryker / coverage exclusions are bounded; do not invent a new category).
>    - **`scripts/ci/assert_merged_line_coverage_min.py`** (the script that enforces the floor).
>    - **`scripts/ci/refresh_stryker_baselines.py`** and **`scripts/ci/assert_stryker_score_vs_baseline.py`** (Stryker baseline mechanics).
>    - **`stryker-config.application.json`** as the template for the new **`stryker-config.api.json`**.
>    - **`docs/BILLING.md`** §§ component breakdown, security model, operational considerations.
>    - **`docs/AZURE_MARKETPLACE_SAAS_OFFER.md`** (the GA-flag table — webhook actions today vs after flip).
>    - **`ArchLucid.Api/appsettings.json`** and any `appsettings.*.sample.json` Marketplace fragments.
>    - **`ArchLucid.Api/Controllers/`** + **`ArchLucid.Api/Startup/`** + **`ArchLucid.Api/Middleware/`** to identify the lowest-hanging untested surfaces (the assessment and `CODE_COVERAGE.md` already point at: `JobsController`, `DocsController.ReplayRecipes`, `ScopeDebugController`, `AuthDebugController`, `DemoController`, `MeteringAdminController`, `ApiPaging.TryParseUtcTicksIdCursor`, `RetrievalController.Search`, `TenantTrialController.GetTrialStatusAsync`, `FileWithRangeResult`, `ApiRequestMeteringMiddleware`, `TrialSeatReservationMiddleware`).
>
> 2. **Identify the coverage gap quantitatively** before writing tests. From a clean Release build of tests:
>    ```
>    dotnet test ArchLucid.sln -c Release --settings coverage.runsettings --collect:"XPlat Code Coverage" --results-directory ./coverage-raw
>    python scripts/ci/coverage_cobertura.py ./coverage-raw
>    ```
>    Read the merged Cobertura report (or `_cov_merge`/`coverage-raw` folder already present) and write a one-paragraph note to `docs/CODE_COVERAGE.md` listing the **specific `ArchLucid.Api` namespaces** below 79 % so reviewers can audit your test selection.
>
> 3. **Add unit tests for `ArchLucid.Api`** in **`ArchLucid.Api.Tests/Unit/`** (one test class per controller/middleware, one file per class — house rule). Cover at minimum:
>    - **Controllers:** `JobsController` (success + 400 + 404 + RBAC), `DocsController.ReplayRecipes` (404 vs 200), `ScopeDebugController.GetScope` (with and without scope context), `AuthDebugController.Me` (DevelopmentBypass + ApiKey + JwtBearer paths via `WebApplicationFactory` overrides), `DemoController.SeedAsync` (gated by `Demo:Enabled` + `Demo:SeedOnStartup`), `MeteringAdminController.GetTenantSummaryAsync`, `RetrievalController.Search` (TopK clamp + validation), `TenantTrialController.GetTrialStatusAsync` (not-found / none / active), every branch of `FileWithRangeResult.ExecuteResultAsync` (empty / full / range / out-of-range).
>    - **Middleware:** `ApiRequestMeteringMiddleware` (off, path-filter, empty tenant, success, swallowed `RecordAsync` failure), `TrialSeatReservationMiddleware` (skip paths, anonymous, no-principal-key, `sub` vs `objectidentifier` reservation, `TrialLimitExceededException` → 402), correlation-id middleware (header echo + generated id).
>    - **Helpers:** `ApiPaging.TryParseUtcTicksIdCursor` (valid / invalid / null / boundary), pagination defaults clamping, problem+json envelope builder.
>    - **No `ConfigureAwait(false)` in tests** (workspace user rule).
>    - **`is null`** for null checks; **same-line guard clauses**; primary constructors on the test fixtures.
>
> 4. **Stryker on `ArchLucid.Api`.** Create **`stryker-config.api.json`**:
>    ```json
>    {
>      "stryker-config": {
>        "project": "ArchLucid.Api/ArchLucid.Api.csproj",
>        "test-projects": ["ArchLucid.Api.Tests/ArchLucid.Api.Tests.csproj"],
>        "reporters": ["progress", "html", "json"],
>        "thresholds": { "high": 70, "low": 55, "break": 55 }
>      }
>    }
>    ```
>    Honest threshold floor first (HTTP wiring code is mutation-rich); ratchet later via the existing **`scripts/ci/refresh_stryker_baselines.py`** workflow. Update **`docs/MUTATION_TESTING_STRYKER.md`** with the new config, the rationale for the lower starting floor, and the ratchet plan; update **`scripts/ci/refresh_stryker_baselines.py`** if it enumerates configs by name. Add an **advisory** Stryker job for `ArchLucid.Api` to **`.github/workflows/ci.yml`** (or extend an existing Stryker job) that runs the new config and uploads the html report — do **not** make it merge-blocking yet.
>
> 5. **Marketplace GA flip.**
>    - In **`ArchLucid.Api/appsettings.json`** (and any `appsettings.Production.*.sample.json`), change **`Billing:AzureMarketplace:GaEnabled`** to **`true`**.
>    - Remove the `AcknowledgedNoOp` early-return on `ChangePlan` / `ChangeQuantity` from `AzureMarketplaceBillingProvider` (the GA branch already exists per `docs/BILLING.md`); keep the **idempotency** path on `dbo.BillingWebhookEvents` (PK on `EventId`) and the **Service Bus publish** of `com.archlucid.billing.marketplace.webhook.received.v1` exactly as documented in `docs/BILLING.md` and ADR 0019.
>    - Update **`docs/AZURE_MARKETPLACE_SAAS_OFFER.md`** so the webhook-action table reflects the new default (200 + applied, not 202 + `AcknowledgedNoOp`); keep an explicit note that operators can set **`GaEnabled=false`** in non-production for isolated tests without network.
>    - Add a new runbook **`docs/runbooks/MARKETPLACE_CHANGEPLAN_QUANTITY_ROLLBACK.md`** that documents: (a) how to flip **`GaEnabled=false`** at the App Configuration / appsettings layer without redeploying, (b) how to re-process a webhook from `dbo.BillingWebhookEvents`, (c) how to reconcile `Tier` / `SeatsPurchased` if a `ChangePlan` mis-mapped, (d) the exact App Insights / Grafana queries to confirm the rollback. Include "first 5 minutes" commands at the top per the assessment supportability recommendation.
>    - Add **integration tests** in `ArchLucid.Api.Tests` that POST `ChangePlan` and `ChangeQuantity` with a Microsoft-issued JWT (use the same JWT-mint pattern as `scripts/ci/mint_ci_jwt.py`) and assert: row mutated, audit emitted, integration event published exactly once, second identical webhook returns 200 idempotent.
>
> 6. **Verify the gate locally.** Run:
>    ```
>    dotnet test ArchLucid.sln -c Release --settings coverage.runsettings --collect:"XPlat Code Coverage" --results-directory ./coverage-raw
>    python scripts/ci/assert_merged_line_coverage_min.py ./coverage-raw/**/coverage.cobertura.xml --min-line-pct 79 --min-branch-pct 63 --min-package-line-pct 63
>    ```
>    The script must exit **0** with **no `--skip-package-line-gate`**.
>
> 7. **Docs.** Update:
>    - **`docs/CODE_COVERAGE.md`** — replace the "**~60 %** per-package line on `ArchLucid.Api`" line with the new measured number, and remove the "expect `dotnet-full-regression` to fail until tests lift those numbers" sentence (or rewrite it accurately).
>    - **`docs/CHANGELOG.md`** — single `## Unreleased` bullet covering coverage lift, Stryker config, and Marketplace GA flip with rollback runbook link.
>    - **`docs/MUTATION_TESTING_STRYKER.md`** — list the new config and ratchet target.
>
> **House-style guardrails:**
> - C#: primary constructors, expression-bodied members, same-line guard clauses, `is null`, LINQ pipelines, one class per file, blank line before each `if` / `foreach` (except first line of a method), no `var` (concrete types), no `ConfigureAwait(false)` in tests.
> - Always check nulls; new test fixtures take a `CancellationToken` where the SUT does.
> - Marketplace webhook validation must keep `Billing:AzureMarketplace:OpenIdMetadataAddress` + `ValidAudiences` JWT validation — do not bypass crypto verification on any branch.
>
> **Acceptance criteria (verify before declaring done):**
> - `python scripts/ci/assert_merged_line_coverage_min.py … --min-package-line-pct 63` exits **0** for `ArchLucid.Api` with **no waiver flag** and merged line is **≥ 79 %**, merged branch **≥ 63 %**.
> - `stryker-config.api.json` exists; running it locally produces an html report; the new advisory CI step uploads the report as an artifact.
> - `Billing:AzureMarketplace:GaEnabled=true` is the shipped default in `ArchLucid.Api/appsettings.json`; `AzureMarketplaceBillingProvider` no longer returns `AcknowledgedNoOp` on `ChangePlan` / `ChangeQuantity`; integration tests prove a row is mutated and an event published exactly once on duplicate delivery.
> - `docs/runbooks/MARKETPLACE_CHANGEPLAN_QUANTITY_ROLLBACK.md` exists, is linked from `docs/AZURE_MARKETPLACE_SAAS_OFFER.md` and from the `ARCHITECTURE_INDEX.md` runbooks section.
> - `docs/CODE_COVERAGE.md`, `docs/MUTATION_TESTING_STRYKER.md`, and `docs/CHANGELOG.md` are updated in the same PR; the `audit-core-const-count` style anchor in `docs/AUDIT_COVERAGE_MATRIX.md` is **not** edited (no audit constant change).
> - `dotnet build ArchLucid.sln`, `dotnet test ArchLucid.sln` (Core + Integration + SQL), and `cd archlucid-ui && npm test && npm run build` all succeed.

---

## Prompt 4 — Close the security trust gap: pen test, full-RLS expansion, and LLM prompt PII redaction

**Improvement 5** in the assessment.

**Quality lift:** Security (82 → ~88), Auditability (88 → ~91), Marketability (78 → ~82 via the `−25 %` trust-discount line in `PRICING_PHILOSOPHY.md` § 5.1 becoming closeable).

### Paste this into Cursor Agent

> **Goal.** Close **three** documented security gaps from **`docs/security/SYSTEM_THREAT_MODEL.md`** § 8 ("Gaps to track in backlog") in one coordinated security sprint:
>
> 1. **Commission a third-party pen test** of the production deployment posture — *land the engagement scaffolding* in this PR (statement of work template, scope doc, redacted-summary template, publication path). Do not invent a vendor; leave fields placeholder until product/security signs the SoW.
> 2. **Expand SQL Row-Level Security to every tenant-scoped table** (today RLS covers "every authority table that carries the scope triple on the row" per **`docs/security/MULTI_TENANT_RLS.md`** § 2; the threat model still calls expansion an open gap). Identify uncovered tables, add the predicate-policy migrations, and **close** the corresponding rows in **`docs/security/RLS_RISK_ACCEPTANCE.md`**.
> 3. **Add LLM prompt PII / secrets redaction** as the first defense layer for the LLM hot path (per the threat model row "API → LLM" → "PII / secrets in prompts" mitigation: "optional prompt redaction backlog"). Begin as a deny-list (tenant secret patterns + common PII regexes) gated by a `LlmPromptRedaction:Enabled` flag, with the **safe default** being `Enabled=true` in production and `false` only when explicitly overridden.
>
> **Non-goals.** Do not engage a vendor or sign a SoW from inside this PR. Do not edit historical migrations (DbUp `001`–latest). Do not weaken the existing `dbo.AuditEvents` `DENY UPDATE/DELETE` posture. Do not introduce model-side filtering inside Azure OpenAI configuration — keep redaction at the application boundary so it remains auditable in tests.
>
> **Steps (do them yourself; do not delegate to subagents):**
>
> 1. **Read first:**
>    - **`docs/security/SYSTEM_THREAT_MODEL.md`** (gap list, mitigations table).
>    - **`docs/security/MULTI_TENANT_RLS.md`** (covered tables, `SESSION_CONTEXT` predicate pattern, bypass policy).
>    - **`docs/security/RLS_RISK_ACCEPTANCE.md`** (template / register).
>    - **`docs/AUDIT_COVERAGE_MATRIX.md`** (the `dbo.AuditEvents` `DENY UPDATE/DELETE` pattern — copy the same idempotent `DENY` block style).
>    - **`ArchLucid.Persistence/Migrations/036_RlsArchiforgeTenantScope.sql`** (the canonical RLS migration whose pattern you will mirror; **do not edit it**).
>    - **`ArchLucid.Persistence/Scripts/ArchLucid.sql`** (the consolidated DDL — your additions must land here too, per the workspace rule).
>    - **`ArchLucid.Application`** (or wherever `IAgentExecutor` / `RealAgentExecutor` lives) and the LLM completion provider (`DelegatingLlmCompletionProvider`, `NullContentSafetyGuard`) to find the seam where prompts are assembled.
>    - **`docs/AGENT_TRACE_FORENSICS.md`** (so redaction is visible in trace blobs the same way it is visible in prompts).
>
> 2. **Pen-test scaffolding (Markdown only).** Create:
>    - **`docs/security/PEN_TEST_SOW_TEMPLATE.md`** — vendor placeholder, scope (Container Apps + API + Worker + UI + Logic Apps + APIM + Front Door), out-of-scope list (third-party deps, social engineering — mirror `SECURITY.md` "Out of scope"), test environment expectations (Azure subscription, ephemeral tenant), data-handling rules (no real customer data, scrubbed seeds), report deliverable shape.
>    - **`docs/security/PEN_TEST_REDACTED_SUMMARY_TEMPLATE.md`** — public-facing template that becomes the actual published report; sections: scope tested, methodology, severity-by-class summary, remediations landed, residual risks accepted (cross-link `RLS_RISK_ACCEPTANCE.md`).
>    - **`docs/go-to-market/TRUST_CENTER.md`** update — add a "Penetration testing" subsection that links to the redacted-summary template (until a real report exists) and references the `−25 %` trust discount work-down row in `PRICING_PHILOSOPHY.md § 5.3` (created in Prompt 1).
>    - **`docs/CHANGELOG.md`** entry: *Pen-test SoW + redacted-summary templates landed; first redacted summary will publish under `docs/security/pen-test/`*.
>
> 3. **RLS expansion.**
>    - Run the inventory: list **every `dbo.*` table that holds tenant-scoped data** (any of `TenantId`, `WorkspaceId`, `ProjectId`) and is **not** already protected by an RLS policy in `036_RlsArchiforgeTenantScope.sql` (or any later RLS migration). The scope-context columns are documented in **`docs/security/MULTI_TENANT_RLS.md`** § 4–§ 6. Reuse `scripts/ci/assert_tenant_inventory_tables_in_archlucid_sql.py` as a model if useful.
>    - Add a **single new forward migration** **`0NN_RlsTenantScopeExpansion.sql`** (next free DbUp number per `docs/SQL_SCRIPTS.md`) that:
>      - Creates one **predicate function** *if not already present* (`fn_TenantScopePredicate(@TenantId, @WorkspaceId, @ProjectId)`) with the same `SESSION_CONTEXT('af_tenant_id')` / `af_workspace_id` / `af_project_id` shape used in `036_*`.
>      - Adds a `SECURITY POLICY` per uncovered table with both `FILTER PREDICATE` and `BLOCK PREDICATE` (`AFTER INSERT`, `AFTER UPDATE`).
>      - Is **idempotent** (`IF NOT EXISTS` guards) and skips tables where the role `ArchLucidApp` does not yet exist (same skip pattern as `051_AuditEvents_DenyUpdateDelete.sql` per `docs/AUDIT_COVERAGE_MATRIX.md`).
>    - **Mirror the migration** into **`ArchLucid.Persistence/Scripts/ArchLucid.sql`** after the corresponding table DDL — workspace rule "All SQL DDL should be in a single file for each database".
>    - Update **`docs/security/MULTI_TENANT_RLS.md`** § 5 (or § 9 child-table list) to reflect the new coverage and to remove the corresponding "open" rows.
>    - Update **`docs/security/RLS_RISK_ACCEPTANCE.md`** by **closing** every entry whose table is now covered; add a closure date and link to the new migration. Do not delete history — change `Status: Accepted` to `Status: Resolved YYYY-MM-DD (migration 0NN)`.
>    - Add **integration tests** in `ArchLucid.Persistence.Tests` (per-test database via the existing fixture) that for each newly-covered table:
>      - With `SESSION_CONTEXT` set to tenant A, a row inserted as tenant A is visible.
>      - With `SESSION_CONTEXT` swapped to tenant B, the same row returns **zero rows**.
>      - With **no** `SESSION_CONTEXT`, the table returns **zero rows** (deny-by-default per `MULTI_TENANT_RLS.md` § 3).
>
> 4. **LLM prompt redaction.**
>    - Create **`ArchLucid.Core/Llm/Redaction/IPromptRedactor.cs`** (interface) and **`PromptRedactor.cs`** (default implementation). One class per file. Primary constructor over `IOptions<LlmPromptRedactionOptions>` and `ILogger<PromptRedactor>`.
>    - **`LlmPromptRedactionOptions`** in its own file with: `bool Enabled` (default `true`), `IReadOnlyList<string> DenyListRegexes` (seed: email, US SSN, credit-card-shape, JWT-shape `eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}`, generic API-key prefix `[A-Za-z0-9]{32,}`), `string ReplacementToken` (default `[REDACTED]`).
>    - The redactor takes a string, runs each regex (compiled once, `RegexOptions.Compiled | RegexOptions.CultureInvariant`), and returns the redacted string + a count of redactions per category for telemetry.
>    - Wire it into the LLM hot path in front of `DelegatingLlmCompletionProvider` (or whichever class assembles the final prompt) so **system prompt**, **user prompt**, and **agent context** are all redacted before the HTTP call to Azure OpenAI. Keep the original (un-redacted) prompt **out of** the trace blob — the trace must contain the redacted version. (See `docs/AGENT_TRACE_FORENSICS.md` for trace-blob mechanics; redaction must be applied **before** the blob is written.)
>    - Add metrics on the **`ArchLucid`** meter: counter **`archlucid_llm_prompt_redactions_total`** with label **`category`** (`email | ssn | credit_card | jwt | api_key | custom`); counter **`archlucid_llm_prompt_redaction_skipped_total`** when **`Enabled=false`** (so operators can audit deliberate disablement).
>    - Update **`ArchLucid.Core/Diagnostics/ArchLucidInstrumentation.cs`** with the new instruments (XML doc comments — workspace rule).
>    - Update **`docs/OBSERVABILITY.md`** with the new instruments, and **`docs/security/SYSTEM_THREAT_MODEL.md`** § 5 to mark the "API → LLM" → "I" cell as **mitigated** (default-on).
>    - **Tests** in `ArchLucid.Core.Tests` (or appropriate Tests assembly): each regex matches its intended payload and does not match obvious false positives; `Enabled=false` returns the input unchanged and increments the skipped counter; redactor preserves UTF-8 and surrogate pairs; counter labels are stable.
>
> 5. **Top-level docs and runbook.**
>    - Add **`docs/runbooks/LLM_PROMPT_REDACTION.md`** — when to flip `Enabled=false` (never in production without a security ticket), how to add a regex, how to read the redaction counters, how to confirm an outbound prompt was redacted via `archlucid_agent_trace_blob_persist_duration_ms` correlation. Include "first 5 minutes" commands.
>    - Cross-link from **`SECURITY.md`**, **`docs/security/SYSTEM_THREAT_MODEL.md`**, and **`docs/AGENT_TRACE_FORENSICS.md`**.
>    - Add a `## Unreleased` bullet to **`docs/CHANGELOG.md`**.
>
> **House-style guardrails:**
> - C#: primary constructors, expression-bodied members, same-line guard clauses, `is null`, LINQ pipelines, one class per file, blank line before each `if` / `foreach` (except first line of a method), no `var`, no `ConfigureAwait(false)` in tests.
> - SQL: idempotent migration; `WITH SCHEMABINDING` on the predicate function; **mirror into the consolidated `ArchLucid.sql`**; do not modify any historical `00x` / `0xx` migration.
> - Regexes: compiled, culture-invariant, non-backtracking-where-possible; never `.*` over user input; explicit anchors when feasible.
> - Always check nulls; the redactor must short-circuit on `null` / empty string and return the input unchanged with a `0` redaction count.
>
> **Acceptance criteria (verify before declaring done):**
> - **Pen-test SoW + redacted-summary templates** exist; `TRUST_CENTER.md` links to them; `PRICING_PHILOSOPHY.md § 5.3` (added in Prompt 1) references this PR as the trust-discount work-down evidence.
> - **RLS expansion migration** (`0NN_RlsTenantScopeExpansion.sql`) lands; the consolidated `ArchLucid.sql` is updated; `MULTI_TENANT_RLS.md` reflects new coverage; `RLS_RISK_ACCEPTANCE.md` shows resolved entries with dates and migration link.
> - **Per-newly-covered-table integration tests** prove tenant-A visibility, tenant-B isolation, and no-context deny-by-default.
> - **`PromptRedactor`** is wired into the LLM hot path; agent trace blobs contain the **redacted** prompt; the new counters are visible on the `ArchLucid` meter.
> - **`Enabled=true` is the production default**; flipping to `false` requires an explicit appsettings override and emits a startup warning log.
> - `dotnet test ArchLucid.sln` (Core + Integration + SQL) is green; `cd archlucid-ui && npm test` is green; existing `audit-core-const-count` CI anchor is unchanged (no audit-event-type churn from this PR).
> - `docs/runbooks/LLM_PROMPT_REDACTION.md`, `docs/CHANGELOG.md`, and the cross-links in `SECURITY.md` / `SYSTEM_THREAT_MODEL.md` / `AGENT_TRACE_FORENSICS.md` are landed in the same PR.

---

## Prompt 5 — Collapse the test-script matrix and stand up a single Concept Map

**Improvement 6** in the assessment.

**Quality lift:** Cognitive Load (58 → ~70), Maintainability (72 → ~78), Documentation (90 → ~93 via editorial discipline), Supportability (82 → ~85 via one entry point operators can remember).

### Paste this into Cursor Agent

> **Goal.** Reduce the operator + contributor cognitive surface in two complementary ways:
>
> 1. **Collapse `test-*.cmd` + `test-*.ps1`** (a 16-file matrix today: `test-core`, `test-fast-core`, `test-integration`, `test-sqlserver-integration`, `test-slow`, `test-full`, `test-ui-smoke`, `test-ui-unit` — each in both `.ps1` and `.cmd`) into a **single parameterized `test.ps1 -Tier <core|fast-core|integration|sql|full|ui-smoke|ui-unit>`** entry point with a thin **`test.cmd`** trampoline. Keep behavior identical to today's scripts; this is a refactor, not a re-design.
> 2. **Stand up `docs/CONCEPTS.md`** as the **canonical concept map** — at most twelve named concepts a contributor must own (Run, Authority Pipeline, Manifest, Finding Engine, Comparison Record, Replay Verify Mode, Scope Triple, RLS, Audit Event, Policy Pack, Layer (Core Pilot / Advanced / Enterprise), Provider Abstraction). Add a **CI guard** that flags new docs which introduce capitalized concept words not listed in `CONCEPTS.md`, so vocabulary drift is detectable on PR.
>
> **Non-goals.** Do not change what each test tier actually runs (no filter changes, no exclusions added or removed). Do not delete any runbook. Do not rename existing concepts inside the codebase. Do not edit historical archive docs.
>
> **Steps (do them yourself; do not delegate to subagents):**
>
> 1. **Read first:**
>    - Every existing **`test-*.ps1`** and **`test-*.cmd`** at the repo root. Note that several are one-liners (`test-core.ps1` is six lines wrapping `dotnet test ArchLucid.sln --filter "Suite=Core"`); others have richer SQL / UI logic. Preserve every script's **exact filter string**, **exact `Set-StrictMode` / `$ErrorActionPreference`** posture, and **exit-code propagation**.
>    - **`docs/TEST_STRUCTURE.md`** and **`docs/TEST_EXECUTION_MODEL.md`** — these are the canonical tier list; the new entry point must use the **same tier names**.
>    - **`README.md`** "Running Tests" and **`docs/OPERATOR_QUICKSTART.md`** — these reference today's scripts and must be updated.
>    - **`docs/GLOSSARY.md`** — the concept overlap with the new `CONCEPTS.md` must be intentional (`CONCEPTS.md` is the **short** map; `GLOSSARY.md` stays the **full** dictionary).
>    - **`docs/ARCHITECTURE_INDEX.md`** — link the new `CONCEPTS.md` from the Orientation section.
>
> 2. **Build the new entry point.**
>    - Create **`test.ps1`** at the repo root: `Set-StrictMode -Version Latest`, `$ErrorActionPreference = 'Stop'`, `param([Parameter(Mandatory=$true)][ValidateSet('core','fast-core','integration','sql','full','ui-smoke','ui-unit')][string]$Tier, [string]$AdditionalArgs = '')`. Body: a `switch ($Tier)` (per the **`CSharp-Terse-03-SwitchExpressions`** rule's spirit, applied to PowerShell as a clean `switch`) that dispatches to the canonical `dotnet test` / `npm` invocation that the corresponding old script uses today.
>    - Create **`test.cmd`** as a one-line trampoline: `@powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0test.ps1" -Tier %1 %*` so cmd users keep working.
>    - **Delete** `test-core.ps1` / `.cmd`, `test-fast-core.ps1` / `.cmd`, `test-integration.ps1` / `.cmd`, `test-sqlserver-integration.ps1` / `.cmd`, `test-slow.ps1` / `.cmd`, `test-full.ps1` / `.cmd`, `test-ui-smoke.ps1` / `.cmd`, `test-ui-unit.ps1` / `.cmd`.
>    - **Grep the repo for every reference** to the deleted script names (`Grep` tool, not `rg` shelling out): docs, CI workflows, devcontainer, README, archive — **do not edit `docs/archive/*`**. Replace each live reference with `test.ps1 -Tier <…>` (or `test.cmd <tier>` where cmd is required, e.g. some Windows CI steps).
>    - In **`.github/workflows/ci.yml`**, replace each `./test-*.ps1` / `./test-*.cmd` invocation with the new entry point, keeping the same matrix shape and the same job names — CI should be a no-op semantic change.
>
> 3. **Concept map.**
>    - Create **`docs/CONCEPTS.md`** with at most twelve concepts. For each: a one-sentence definition, the canonical doc to read next (link), and the canonical code home (file or namespace). Suggested set (final list is the contributor's call): **Run**, **Authority Pipeline**, **Manifest** (golden), **Finding Engine**, **Comparison Record**, **Replay Verify Mode**, **Scope Triple** (`tenant / workspace / project`), **RLS** (`SESSION_CONTEXT`-driven), **Audit Event**, **Policy Pack**, **Layer** (Core Pilot / Advanced Analysis / Enterprise Controls), **Provider Abstraction** (billing, IdP, agent execution).
>    - Header rule: *"This file is the **short** concept map. The full dictionary is `GLOSSARY.md`. Adding a new concept here requires updating the contributor onboarding docs."*
>    - Cross-link from **`README.md`** "Getting started" and from **`docs/ARCHITECTURE_INDEX.md`** Orientation.
>
> 4. **CI guard for vocabulary drift.**
>    - Add **`scripts/ci/check_concept_vocabulary.py`** — Python 3.11+, no third-party deps, < 120 lines. Behavior:
>      - Reads `docs/CONCEPTS.md`, extracts the bold concept names (`**Concept Name**` rows in the table).
>      - Reads every **non-archive** `docs/**/*.md` and `README.md`.
>      - Flags **capitalized multi-word phrases** that look like concept names (heuristic: two-or-more consecutive `[A-Z][a-z]+` tokens, optionally separated by spaces) which appear **≥ N** times across the docs but are **not** in the concept map.
>      - Allow-list: a small inline set of obvious non-concepts (`Azure Front Door`, `Azure Container Apps`, `SQL Server`, `Service Bus`, `Application Insights`, etc.) plus product names from `docs/go-to-market/COMPETITIVE_LANDSCAPE.md` (`LeanIX`, `Ardoq`, `MEGA HOPEX`, `Sparx EA`, `ServiceNow CSDM`).
>      - Default mode: **warn-only** (exit 0, print `::warning::` annotations for GitHub Actions). A `--strict` mode exits non-zero so we can ratchet later.
>      - Has a unit-testable `find_unknown_concepts(concepts: set[str], doc_text: str, allow_list: set[str], min_occurrences: int) -> list[str]` function and a tiny inline doctest.
>    - Wire into **`.github/workflows/ci.yml`** as a **non-blocking** doc-sanity step (`continue-on-error: true`) alongside `check_doc_links.py`.
>    - Document the guard in **`docs/CONCEPTS.md`** ("CI guard" section).
>
> 5. **Doc updates.**
>    - **`README.md`** "Running Tests" — replace each `test-*.cmd` reference with the new entry point. Keep the three example invocations identical in **what they do**, just change **how they are typed**.
>    - **`docs/OPERATOR_QUICKSTART.md`**, **`docs/TEST_STRUCTURE.md`**, **`docs/TEST_EXECUTION_MODEL.md`** — same rewrite; preserve the canonical tier names.
>    - **`docs/CONTRIBUTOR_ONBOARDING.md`** and **`docs/onboarding/day-one-developer.md`** — point at the new entry point and at `CONCEPTS.md`.
>    - **`docs/CHANGELOG.md`** — single `## Unreleased` bullet covering the script consolidation, the concept map, and the new CI guard.
>
> **House-style guardrails:**
> - PowerShell: `Set-StrictMode -Version Latest`, `$ErrorActionPreference = 'Stop'`, explicit `param(…)` validation, return `$LASTEXITCODE`.
> - Python: 3.11+, `argparse`, no third-party deps, type hints, one `main()` and one pure function for the heuristic so the unit test stays trivial.
> - Markdown: no emoji unless the user asks. Tables short and scannable. `CONCEPTS.md` ≤ ~150 lines.
> - **Workspace rule "Markdown-Generosity"** still applies — this prompt produces three new docs (`CONCEPTS.md`, the `## CI guard` section inside it, and the changelog bullet). Do not over-fragment further.
>
> **Acceptance criteria (verify before declaring done):**
> - The eight `test-*.ps1` and eight `test-*.cmd` scripts at the repo root are **deleted**; **`test.ps1`** and **`test.cmd`** exist; no live (non-archive) doc, CI workflow, or devcontainer file references the deleted names.
> - Each tier produces the **same `dotnet test` / `npm` invocation** as today (verify by hand-comparing the dispatched command for each `-Tier` value to the corresponding deleted script).
> - `docs/CONCEPTS.md` exists, is linked from `README.md` "Getting started" and `docs/ARCHITECTURE_INDEX.md` Orientation, and lists at most twelve concepts.
> - `scripts/ci/check_concept_vocabulary.py` runs locally, prints zero warnings against the current docs after vocabulary in `CONCEPTS.md` is set (allow-list iteration is allowed); has a passing unit test for `find_unknown_concepts`.
> - The new CI step appears in `.github/workflows/ci.yml` under `continue-on-error: true`.
> - `dotnet test ArchLucid.sln` (Core + Integration + SQL) and `cd archlucid-ui && npm test` are unchanged in behavior; `dotnet build ArchLucid.sln` succeeds.
> - `docs/CHANGELOG.md` has a single new `## Unreleased` bullet describing all three changes.

---

## Process notes

- **Do not delegate any of these prompts to a subagent.** The workspace rule **`Do-The-Work-Yourself.mdc`** applies — it forbids `Task` with `subagent_type` of `generalPurpose`, `explore`, `shell`, or `best-of-n-runner` for implementation work.
- Each prompt should be **one Agent session ending in a single PR**; if the session needs to break, capture interim state via a `TodoWrite` list rather than spawning parallel agents.
- Acceptance criteria are written so a reviewer can mechanically check them; do not weaken them in flight.
- **Suggested PR order:** Prompt 3 (correctness lift unlocks the GA-flip claim) → Prompt 4 (security trust gap, which depends on `PRICING_PHILOSOPHY.md § 5.3` from Prompt 1) → Prompt 5 (script + concept consolidation, which is independent and safe to land any time).
