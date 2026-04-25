> **Scope:** ArchLucid documentation index - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


## ArchLucid documentation index

**Layout note (2026-04-23):** Most engineering pages that historically lived as `docs/<FILE>.md` now live under **`docs/library/<FILE>.md`**. Unless a path is explicitly one of the small `docs/` root spine files (`START_HERE`, `INSTALL_ORDER`, `FIRST_30_MINUTES`, `CORE_PILOT`, `ARCHITECTURE_ON_ONE_PAGE`, `PENDING_QUESTIONS`, …), read backtick paths below as **`docs/library/...`** — full listing in [`DOC_INVENTORY_2026_04_23.md`](library/DOC_INVENTORY_2026_04_23.md).

### Orientation

- **Architecture poster (canonical C4 + ownership + happy path)** — read this before the long-form architecture set  
  - `docs/ARCHITECTURE_ON_ONE_PAGE.md`
- **Operator atlas (canonical route × API × CLI map)** — single table for operator surfaces  
  - `docs/OPERATOR_ATLAS.md`
- **V1 scope contract (product boundary for pilots and releases)** — what is in/out of V1, operator happy path, minimum release criteria  
  - `docs/V1_SCOPE.md`
- **V1 release checklist (actionable gates before handoff)** — scope freeze, deploy, health, operator flow, exports, naming, support bundle, recovery, deferrals  
  - `docs/V1_RELEASE_CHECKLIST.md`
- **V1 deferred / exploratory (doc inventory for intentional scope)** — audit gaps, 59R deferrals, Phase 7 rename, infra polish, `NEXT_REFACTORINGS` boundary  
  - `docs/V1_DEFERRED.md`
- **Start here (canonical buyer + evaluator spine)** — Day-0 journey, five-document table, and where depth moved after the 2026-04-23 doc-root cap  
  - `docs/START_HERE.md` (**canonical**); thin redirects: `docs/FIRST_5_DOCS.md`, `docs/FIRST_FIVE_DOCS.md`, `docs/FIRST_RUN_WIZARD.md`, `docs/FIRST_RUN_WALKTHROUGH.md` (full bodies under `docs/library/`); archived pre-spine table: `docs/archive/FIRST_FIVE_DOCS_SUPERSEDED_2026_04_22.md`
- **Developer Day-1 (week one)** — toolchain, local API + SQL, Core tests, small PR  
  - `docs/onboarding/day-one-developer.md`
- **SRE / Platform Day-1** — health, deploy order, Terraform validate, migrations posture  
  - `docs/onboarding/day-one-sre.md`
- **Security Day-1** — trust boundaries, authZ, RLS, supply chain  
  - `docs/onboarding/day-one-security.md`
- **Operator quickstart (commands)** — health, curl, CLI, smoke/tests  
  - `docs/OPERATOR_QUICKSTART.md`
- **Superseded long-form golden paths (archived 2026-04-17)**  
  - `docs/archive/ONBOARDING_GOLDEN_PATH_2026_04_17.md`, `docs/archive/ONBOARDING_GOLDEN_CHANGE_PATH_2026_04_17.md`
- **Reference SaaS Terraform apply order (Azure roots)**  
  - `docs/REFERENCE_SAAS_STACK_ORDER.md`
- **Week-one role tickets (dev / SRE / security)** — 3–5 checkboxes each  
  - `docs/onboarding/README.md`
- **C4 diagrams for exec/security (PNG + `.mmd` sources)**  
  - `docs/diagrams/c4/README.md`
- **C4 model (Structurizr DSL — system context + containers)**  
  - `docs/c4/README.md`, `docs/c4/workspace.dsl`
- **Request happy path (client → API → SQL → agents)** — stub → developer Day-1 + `API_CONTRACTS`; archived walkthrough  
  - `docs/ONBOARDING_HAPPY_PATH.md`, `docs/archive/ONBOARDING_HAPPY_PATH_2026_04_17.md`
- **System map (Mermaid flows + entry points)**  
  - `docs/SYSTEM_MAP.md`
- **One-page system view (nodes/edges/ops — flowchart narrative)**  
  - `docs/ARCHITECTURE_ON_A_PAGE.md` — superseded as *first* architecture entry by **`ARCHITECTURE_ON_ONE_PAGE.md`** (C4 poster); kept for objective/constraint narrative overlap.
- **Bounded context map (domain boundaries + Mermaid)**  
  - `docs/bounded-context-map.md`
- **Solution project map (`ArchLucid.*` ↔ contexts)**  
  - `docs/PROJECT_MAP.md`
- **API controller area map (logical grouping without folder churn)**  
  - `docs/CONTROLLER_AREA_MAP.md` (canonical); `docs/API_CONTROLLER_MAP.md` (filename alias)
- **API versioning (`Asp.Versioning`, v1 routes, v2 guidance)**  
  - `docs/API_VERSIONING.md`
- **Code map (where to open first)**  
  - `docs/CODE_MAP.md`
- **Context** – high-level purpose and boundary  
  - `docs/ARCHITECTURE_CONTEXT.md`
- **Containers** – projects and their responsibilities  
  - `docs/ARCHITECTURE_CONTAINERS.md`
- **Components** – key modules inside each container  
  - `docs/ARCHITECTURE_COMPONENTS.md`
- **Architecture constraint tests** – NetArchTest + assembly-reference rules (`ArchLucid.Architecture.Tests`, `Suite=Core`)  
  - `docs/ARCHITECTURE_CONSTRAINTS.md`
- **Dual pipeline navigator** – Coordinator (string run) vs Authority (ingestion) paths, shared artifacts, `RunEventTrace` vs `RuleAuditTrace` (JSON still one `DecisionTrace` envelope with `kind`); now opens with a "Which path do I use?" decision tree and a "Why we have not collapsed these" section linking ADR 0010, ADR 0012, and proposed ADR 0021  
  - `docs/CANONICAL_PIPELINE.md` (operator); `docs/archive/dual-pipeline-navigator-superseded.md` (engineering archive)
- **Coordinator pipeline strangler plan** – proposed phased retirement of the Coordinator interface family in favour of the Authority family; **`Status: Proposed`** — implementation gated on architecture-review acceptance per the ADR's own decision-review gate  
  - `docs/adr/0021-coordinator-pipeline-strangler-plan.md`
- **DI registration map** – `AddArchLucidApplicationServices` order (`ArchLucid.Host.Composition`), `AddArchLucidStorage`, partial `ServiceCollectionExtensions`, config gates  
  - `docs/DI_REGISTRATION_MAP.md`
- **Key flows** – run, export, comparison, replay sequences  
  - `docs/ARCHITECTURE_FLOWS.md`
- **Observability** – `ArchLucid` meter, key histograms/counters, `ActivitySource` names, tag conventions (`archlucid.stage.name`, authority pipeline stages)  
  - `docs/OBSERVABILITY.md`
- **Citation-bound aggregate explanations** – `CitationReference` list on `RunExplanationSummary`  
  - `docs/explainability/CITATION_BOUND_RENDERING.md`
- **Data consistency enforcement (orphan probes + optional quarantine)**  
  - `docs/data-consistency/DATA_CONSISTENCY_ENFORCEMENT.md`
- **Tier-1 support runbook + Grafana starter**  
  - `docs/support/TIER_1_RUNBOOK.md`, `docs/support/GRAFANA_DASHBOARD_TIER_1.json`
- **Pilot / cost deployment companions**  
  - `docs/deployment/PILOT_PROFILE.md`, `docs/deployment/PER_TENANT_COST_MODEL.md`
- **Buyer journey (outside-in)**  
  - `docs/go-to-market/BUYER_JOURNEY.md`, `docs/go-to-market/COMPETITOR_CONTRAST.md`, `docs/go-to-market/NOT_A_FIT.md`
- **Explainability trace coverage + faithfulness heuristic** – trace completeness ratio, aggregate explanation token overlap vs finding traces  
  - `docs/EXPLAINABILITY_TRACE_COVERAGE.md`
- **Code coverage (merged line gate, scripts)**  
  - `docs/CODE_COVERAGE.md`
- **Mutation testing (Stryker) + safe ratchet to higher floors**  
  - `docs/MUTATION_TESTING_STRYKER.md`, `docs/STRYKER_RATchet_TARGET_72.md`
- **System STRIDE threat summary (product boundary)**  
  - `docs/security/SYSTEM_THREAT_MODEL.md`
- **Agent output structural evaluation** – `AgentResult` JSON key completeness, `GET …/agent-evaluation`, optional metrics recorder  
  - `docs/AGENT_OUTPUT_EVALUATION.md`
- **API key rotation (comma-separated overlap)** – `ApiKeys:*` cutover without single-step client flips  
  - `docs/runbooks/API_KEY_ROTATION.md`
- **RLS residual risk acceptance (template)** – governance note for uncovered tables / bypass  
  - `docs/security/RLS_RISK_ACCEPTANCE.md`

### Operator shell (front end)

- **Operator shell guide (55R)** – workflow, artifact review, graph vs compare/replay, run **pipeline timeline** (`AuthorityPipelineTimeline` on run detail), focused UI tests, API expectations  
  - `docs/operator-shell.md`
- **Architecture** – context, containers, components, data flow, security model, operational concerns; **§4a** documents first-wave **role-aware UI shaping** (nav + principal read-model, not entitlements)  
  - `archlucid-ui/docs/ARCHITECTURE.md`
- **Tutorial** – Next.js/React concepts for back-end developers  
  - `archlucid-ui/docs/OPERATOR_SHELL_TUTORIAL.md`
- **Component reference** – every component, prop, and helper library  
  - `archlucid-ui/docs/COMPONENT_REFERENCE.md`
- **Data flow and state** – data flow diagrams, state patterns, templates for new pages  
  - `archlucid-ui/docs/DATA_FLOW_AND_STATE.md`
- **C# to React Rosetta Stone** – side-by-side code for every pattern  
  - `archlucid-ui/docs/CSHARP_TO_REACT_ROSETTA.md`
- **Annotated walkthrough** – line-by-line reading of a real page  
  - `archlucid-ui/docs/ANNOTATED_PAGE_WALKTHROUGH.md`
- **Testing and troubleshooting** – tests, debugging, common issues  
  - `archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md`
- **Data model** – core tables/records and relationships  
  - `docs/DATA_MODEL.md`
- **SQL scripts** – migrations, consolidated SQL Server DDL, DbUp vs Persistence bootstrap  
  - `docs/SQL_SCRIPTS.md`
- **Context ingestion** – connectors, parsers, deduplication, create-run fields  
  - `docs/CONTEXT_INGESTION.md`
- **Knowledge graph** – typed nodes/edges from `ContextSnapshot`, inference, validation, SQL JSON  
  - `docs/KNOWLEDGE_GRAPH.md`

### Decisions and onboarding

- **Glossary** – 20 domain terms (authority run, golden manifest, finding engine, policy pack …)  
  - `docs/GLOSSARY.md`
- **Concepts (canonical vocabulary)** – adjudicates between competing terms (e.g. Microsoft Entra ID over its legacy name); enforced by `scripts/ci/check_concept_vocabulary.py` against `docs/` (excluding `docs/archive/`)  
  - `docs/CONCEPTS.md`
- **Changelog** – per-release summaries (55R → latest); archived design-session logs in `docs/archive/`  
  - `docs/CHANGELOG.md`
- **ADRs** – 11 numbered decisions; no shared prefix IDs  
  - `docs/adr/README.md`
- **Contributor onboarding** – stub → `docs/onboarding/day-one-developer.md`; archived checklist  
  - `docs/CONTRIBUTOR_ONBOARDING.md`, `docs/archive/ONBOARDING_CONTRIBUTOR_ONBOARDING_2026_04_17.md`

### API and contracts

- **HTTP contracts** – status codes, validation, problem details  
  - `docs/API_CONTRACTS.md`
- **Audit retention and export tiers** – hot/warm/cold lifecycle, `GET /v1/audit/export`, Migration **051** append-only, blob archival guidance  
  - `docs/AUDIT_RETENTION_POLICY.md`
- **Alerts** – rules, evaluation, delivery, persistence  
  - `docs/ALERTS.md`
- **Typed findings schema** – payloads and versioning  
  - `docs/FINDINGS_TYPED_SCHEMA.md`, `docs/DECISIONING_TYPED_FINDINGS.md`

### Build, CLI, and operations

- **RTO / RPO targets by environment tier** – SQL HA, geo-replication, production vs dev  
  - `docs/RTO_RPO_TARGETS.md`
- **Geo failover drill (operator checklist)**  
  - `docs/runbooks/GEO_FAILOVER_DRILL.md`
- **Build and run** – configuration, ports, local setup  
  - `docs/BUILD.md`
- **CLI usage** – commands and flags  
  - `docs/CLI_USAGE.md`
- **CLI ↔ API plan** – implementation status and phases  
  - `docs/CLI_API_IMPLEMENTATION_PLAN.md`
- **Demo quickstart**  
  - `docs/demo-quickstart.md`
- **Replay drift runbook**  
  - `docs/RUNBOOK_REPLAY_DRIFT.md`
- **Advisory scan failures** – `docs/runbooks/ADVISORY_SCAN_FAILURES.md`
- **Comparison replay rate limits** – `docs/runbooks/COMPARISON_REPLAY_RATE_LIMITS.md`
- **Provenance / retrieval indexing** – `docs/runbooks/PROVENANCE_INDEXING.md`
- **API key rotation** – `docs/runbooks/API_KEY_ROTATION.md`
- **Marketplace `ChangePlan` / `ChangeQuantity` rollback** – `docs/runbooks/MARKETPLACE_CHANGEPLAN_QUANTITY_ROLLBACK.md` (flip `Billing:AzureMarketplace:GaEnabled=false` without redeploying; supported escape hatch for the 2026-04-20 GA flip)
- **LLM prompt redaction** – `docs/runbooks/LLM_PROMPT_REDACTION.md` (`LlmPromptRedaction`, outbound + trace persistence alignment)
- **Terraform variable sketch (Azure)** – `docs/terraform-azure-variables.md`
- **Infrastructure index (Terraform roots)** – `infra/README.md`
- **Integration event catalog (Service Bus `com.archlucid.*` types + JSON schemas)** – `docs/INTEGRATION_EVENT_CATALOG.md`, `docs/INTEGRATION_EVENT_SCHEMA_REGISTRY.md` (see also `docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md`)
- **Customer trust and access (edge, private data plane, Entra)** – `docs/CUSTOMER_TRUST_AND_ACCESS.md`
- **Azure API Management (Consumption), optional** – `infra/terraform/README.md`
- **Rate limiting / CORS / auth** – see `README.md` and `docs/BUILD.md` (cross-links from backlog `docs/NEXT_REFACTORINGS.md`)

### Contributing and process

- **Test layout** – integration vs unit, traits  
  - `docs/TEST_STRUCTURE.md`
- **Test execution model (54R)** – Core / Fast core / Integration / SQL / Full regression, scripts, CI  
  - `docs/TEST_EXECUTION_MODEL.md`
- **Live API + SQL Playwright** (`ui-e2e-live`, `ui-e2e-live-apikey`, nightly `live-e2e-nightly.yml`)  
  - `docs/LIVE_E2E_HAPPY_PATH.md`  
  - `docs/LIVE_E2E_AUTH_ASSUMPTIONS.md` (DevelopmentBypass vs ApiKey assumptions)  
  - `docs/LIVE_E2E_AUTH_PARITY.md` (CI matrix + roadmap)
- **Formatting** – repo conventions  
  - `docs/FORMATTING.md`
- **Method documentation** – XML doc expectations  
  - `docs/METHOD_DOCUMENTATION.md`
- **Refactoring backlog** – completed batches (§88+) and open ideas  
  - `docs/NEXT_REFACTORINGS.md`
- **Cursor prompts index** – table of all paste-ready prompt docs (links into six-quality, weighted 3–6, SaaS, canonical)  
  - `docs/CURSOR_PROMPTS_INDEX.md`
- **Cursor prompts (six weighted quality improvements)** – paste-ready Agent prompts (k6 gates, audit, coverage, RLS/API keys, rename triage, trace metadata)  
  - `docs/CURSOR_PROMPTS_SIX_QUALITY_IMPROVEMENTS.md`

### How-to guides

- **Comparison replay** – formats, modes, headers, examples  
  - `docs/COMPARISON_REPLAY.md`
- **Add a new comparison type** – step-by-step: type constant → service → replay formatter → DI → tests  
  - `docs/HOWTO_ADD_COMPARISON_TYPE.md`
- **CI migration and seeding regression loop** – pre-push checklist, per-migration checklist, CI YAML  
  - `docs/CI_MIGRATION_CHECKLIST.md`

---

### Archive (`docs/archive/`)

- **Historical change-set logs and superseded notes** — not maintained for current-code accuracy; see folder README  
  - `docs/archive/README.md`

### Typical questions and where to read

- **“How does a run become a manifest?”**  
  → `ARCHITECTURE_FLOWS.md` (Flow A) + `ARCHITECTURE_COMPONENTS.md` (`DecisionEngineService` in `ArchLucid.Decisioning.Merge`) + `KNOWLEDGE_GRAPH.md` (graph → findings → manifest)

- **“How do I replay a comparison and re-export it?”**  
  → `COMPARISON_REPLAY.md` + `ARCHITECTURE_FLOWS.md` (Flow C)

- **“Where do comparison records live and how do I query them?”**  
  → `DATA_MODEL.md` (ComparisonRecords) + `ARCHITECTURE_COMPONENTS.md` (ComparisonRecordRepository, ComparisonReplayService)

- **“Which SQL file runs when? How do I add a migration?”**  
  → `SQL_SCRIPTS.md`

- **“Where should I add a new feature?”**  
  → `ARCHITECTURE_CONTAINERS.md` first, then the relevant section of `ARCHITECTURE_COMPONENTS.md`.

