> **Scope:** Independent first-principles product readiness assessment using a fixed 46-quality weighted model (commercial, enterprise, engineering); scoring reflects in-repo evidence only; intentionally deferred V1.1/V2 scope per [`V1_SCOPE.md`](V1_SCOPE.md) / [`V1_DEFERRED.md`](V1_DEFERRED.md) is excluded from penalties.

# ArchLucid Assessment – Weighted Readiness 73.52%

**Assessment date:** 2026-04-29 (America/New_York).  
**Method:** Scores 1–100 per quality; weights applied exactly as specified; overall readiness = \(\sum_i score_i \cdot weight_i \big/ \sum_i weight_i\).  
**Total model weight:** 102. **Weighted sum:** 7499 → **73.519608%** → **73.52%** (two decimal places).  

**Deferred scope uncertainty:** None material. Explicit V1.1/V2 deferrals (e.g., live Stripe/Marketplace, pen-test publication, PGP drop, MCP membrane, Jira/ServiceNow first-party connectors, Slack connector) are documented in [`docs/library/V1_SCOPE.md`](V1_SCOPE.md) §3 and [`docs/library/V1_DEFERRED.md`](V1_DEFERRED.md).

---

## Executive Summary

### Overall readiness

ArchLucid presents as a **credible V1-shaped systems product**: bounded architecture (API + worker + UI + SQL), heavy automation (tiered .NET CI, merge-blocking live Playwright against SQL-backed API, k6, mutation testing, ZAP/Schemathesis schedules), and unusually strong **documentation / runbook / ADR** discipline. Weighted readiness **73.52%** reflects a mature engineering substrate with **commercial packaging and buyer-motion softness** relative to the engineering depth: category narrative is crowded, the operator surface is broad (Pilot vs Operate disclosure helps but does not eliminate cognitive load), and **sales-led** monetization remains honest against deferred “flip live” gates.

### Commercial picture

The repository supports a grounded buyer story ([`docs/EXECUTIVE_SPONSOR_BRIEF.md`](../EXECUTIVE_SPONSOR_BRIEF.md), [`docs/go-to-market/POSITIONING.md`](../go-to-market/POSITIONING.md), pricing philosophy with CI-enforced single-source pricing). **Proof-of-ROI** is better than typical early-stage B2B thanks to [`docs/library/PILOT_ROI_MODEL.md`](PILOT_ROI_MODEL.md), value-report surfaces, and instrumentation hooks (e.g., optional baseline hours at registration — described in-repo). **Friction remains**: Azure-primary ICP ([`docs/go-to-market/IDEAL_CUSTOMER_PROFILE.md`](../go-to-market/IDEAL_CUSTOMER_PROFILE.md)), explicit disqualifiers (no air-gap / SaaS posture), and a wide feature umbrella increase the burden on qualification and first-session focus. **Commercial packaging readiness** is intentionally incomplete for fully self-serve revenue at scale (live billing/marketplace publication deferred per scope contract — not scored as a V1 defect).

### Enterprise picture

Enterprise-grade **artifacts exist**: Trust Center ([`docs/trust-center.md`](../trust-center.md)), STRIDE summary ([`docs/security/SYSTEM_THREAT_MODEL.md`](../security/SYSTEM_THREAT_MODEL.md)), SOC 2 self-assessment + roadmap, append-only audit design ([`docs/library/AUDIT_COVERAGE_MATRIX.md`](AUDIT_COVERAGE_MATRIX.md)), SQL RLS posture ([`docs/security/MULTI_TENANT_RLS.md`](../security/MULTI_TENANT_RLS.md)), SCIM surface (ADR [`0032`](../adr/0032-scim-v2-service-provider.md)). **Trust gaps are predictable**: no SOC 2 Type II report yet, pen-test outcomes not publicly packaged in V1 contract, and buyers must accept **single-region** posture and integration breadth limits ([`docs/library/BUYER_SCALABILITY_FAQ.md`](BUYER_SCALABILITY_FAQ.md)). Workflow embedding vs incumbent ITSM stacks is **partially addressable via webhooks/API**; first-party Jira/ServiceNow/Slack commitments are explicitly deferred — excluded from penalty.

### Engineering picture

The codebase is **modular and test-heavy** (many bounded projects, repository/service interfaces, golden corpora under `tests/golden-corpus/`, broad integration coverage patterns described in [`docs/library/TEST_STRUCTURE.md`](TEST_STRUCTURE.md)). **Correctness** is strong for deterministic paths (simulator-first CI, replay/drift semantics) but remains inherently constrained anywhere **live LLM variability** touches buyer-visible outputs — partially mitigated via evaluation harnesses and policy gates (files under `ArchLucid.AgentRuntime/` / tests). **Operational maturity** is evidenced by health endpoints, support bundle CLI, correlation IDs, Terraform modules under `infra/`, and extensive runbooks under [`docs/runbooks/`](../runbooks/). Residual risks concentrate in **environment-specific deployment**, **dual-channel logging vs durable audit parity** (honestly flagged in [`docs/library/V1_READINESS_SUMMARY.md`](V1_READINESS_SUMMARY.md)), and **scale promises** (explicit non-promises for multi-region active/active in V1 scope).

---

## Weighted Quality Assessment

**Ordering:** Most urgent → least urgent by **weighted deficiency signal**  
**Weighted deficiency signal** (used for urgency): \(weight \times (100 - score)\)  
**Weighted contribution to readiness:** \(\dfrac{score \cdot weight}{102}\) percentage points of the 73.52% whole  

| Rank | Quality | Score | Weight | Weighted deficiency signal | Weighted contribution to readiness (pp) |
|------|---------|------:|-------:|---------------------------:|----------------------------------------:|
| 1 | Adoption Friction | 62 | 6 | 228 | 45.88 |
| 2 | Marketability | 72 | 8 | 224 | 56.47 |
| 3 | Time-to-Value | 78 | 7 | 154 | 53.53 |
| 4 | Proof-of-ROI Readiness | 76 | 5 | 120 | 37.25 |
| 5 | Differentiability | 70 | 4 | 120 | 29.41 |
| 6 | Workflow Embeddedness | 63 | 3 | 111 | 18.53 |
| 7 | Executive Value Visibility | 74 | 4 | 104 | 29.02 |
| 8 | Usability | 70 | 3 | 90 | 20.59 |
| 9 | Correctness | 78 | 4 | 88 | 30.59 |
| 10 | Trustworthiness | 72 | 3 | 84 | 21.18 |
| 11 | Commercial Packaging Readiness | 58 | 2 | 84 | 11.37 |
| 12 | Security | 76 | 3 | 72 | 22.35 |
| 13 | Interoperability | 68 | 2 | 64 | 13.33 |
| 14 | Decision Velocity | 68 | 2 | 64 | 13.33 |
| 15 | Compliance Readiness | 68 | 2 | 64 | 13.33 |
| 16 | Architectural Integrity | 81 | 3 | 57 | 23.82 |
| 17 | Traceability | 82 | 3 | 54 | 24.12 |
| 18 | Data Consistency | 73 | 2 | 54 | 14.31 |
| 19 | Reliability | 74 | 2 | 52 | 14.51 |
| 20 | Procurement Readiness | 74 | 2 | 52 | 14.51 |
| 21 | AI/Agent Readiness | 76 | 2 | 48 | 14.90 |
| 22 | Explainability | 77 | 2 | 46 | 15.10 |
| 23 | Policy and Governance Alignment | 78 | 2 | 44 | 15.29 |
| 24 | Maintainability | 79 | 2 | 42 | 15.49 |
| 25 | Azure Compatibility and SaaS Deployment Readiness | 80 | 2 | 40 | 15.69 |
| 26 | Auditability | 80 | 2 | 40 | 15.69 |
| 27 | Cognitive Load | 62 | 1 | 38 | 6.08 |
| 28 | Stickiness | 65 | 1 | 35 | 6.37 |
| 29 | Scalability | 68 | 1 | 32 | 6.67 |
| 30 | Cost-Effectiveness | 70 | 1 | 30 | 6.86 |
| 31 | Change Impact Clarity | 70 | 1 | 30 | 6.86 |
| 32 | Template and Accelerator Richness | 72 | 1 | 28 | 7.06 |
| 33 | Availability | 72 | 1 | 28 | 7.06 |
| 34 | Accessibility | 72 | 1 | 28 | 7.06 |
| 35 | Extensibility | 73 | 1 | 27 | 7.16 |
| 36 | Performance | 74 | 1 | 26 | 7.25 |
| 37 | Evolvability | 74 | 1 | 26 | 7.25 |
| 38 | Manageability | 76 | 1 | 24 | 7.45 |
| 39 | Customer Self-Sufficiency | 76 | 1 | 24 | 7.45 |
| 40 | Observability | 77 | 1 | 23 | 7.55 |
| 41 | Deployability | 78 | 1 | 22 | 7.65 |
| 42 | Azure Ecosystem Fit | 81 | 1 | 19 | 7.94 |
| 43 | Supportability | 82 | 1 | 18 | 8.04 |
| 44 | Modularity | 84 | 1 | 16 | 8.24 |
| 45 | Testability | 85 | 1 | 15 | 8.38 |
| 46 | Documentation | 88 | 1 | 12 | 8.63 |

### Per-quality detail (same urgency order)

For each quality below: **Score / Weight / Weighted deficiency signal / Weighted contribution (pp)** are as in the table. Sections **Justification**, **Tradeoffs**, **Improvement recommendations**, and **Fix horizon** follow.

1. **Adoption Friction — 62 / 6 / 228 / 45.88pp**  
   - **Justification:** Broad Operate surface (compare/replay/graph/governance/alerts/Ask) remains discoverable even with progressive disclosure; contributor setup ([`docs/engineering/INSTALL_ORDER.md`](../engineering/INSTALL_ORDER.md)) is non-trivial; ICP disqualifiers shrink addressable market.  
   - **Tradeoffs:** Narrowing the product reduces perceived breadth; widening increases onboarding risk.  
   - **Recommendations:** Instrument “time-to-first-commit” funnel metrics in-product; tighten default tenant/workspace landing to Core Pilot-only tasks; add explicit “skip Operate for now” pathways in UI copy ([`archlucid-ui/`](../../archlucid-ui/README.md) home/chceklists).  
   - **Fix horizon:** v1 (mostly product/copy/analytics).

2. **Marketability — 72 / 8 / 224 / 56.47pp**  
   - **Justification:** Strong internal positioning docs; externally the category (“AI architecture intelligence”) competes with generic Copilot/chat narratives and EAM incumbents ([`docs/go-to-market/COMPETITIVE_LANDSCAPE.md`](../go-to-market/COMPETITIVE_LANDSCAPE.md)).  
   - **Tradeoffs:** Sharp positioning risks excluding buyers; broad positioning sounds undifferentiated.  
   - **Recommendations:** Package three buyer proofs (speed-to-manifest, audit trail, governance gate) into repeatable demo scripts aligned to [`docs/go-to-market/DEMO_QUICKSTART.md`](../go-to-market/DEMO_QUICKSTART.md); ensure `/why` and showcase routes remain evidence-linked, not slogan-only.  
   - **Fix horizon:** v1.

3. **Time-to-Value — 78 / 7 / 154 / 53.53pp**  
   - **Justification:** `archlucid try`, demo surfaces, and Core Pilot checklist ([`docs/CORE_PILOT.md`](../CORE_PILOT.md)) reduce time-to-first-value; real enterprise tenants still pay integration/auth/setup tax.  
   - **Tradeoffs:** Faster paths that skip governance setup can undermine enterprise trust later.  
   - **Recommendations:** Add a “first hour” operator script combining health, sample run, commit, export ([`docs/library/RELEASE_SMOKE.md`](RELEASE_SMOKE.md), [`docs/library/V1_RC_DRILL.md`](V1_RC_DRILL.md)) as the default SE handoff.  
   - **Fix horizon:** v1.

4. **Proof-of-ROI Readiness — 76 / 5 / 120 / 37.25pp**  
   - **Justification:** Measurement narrative exists ([`docs/library/PILOT_ROI_MODEL.md`](PILOT_ROI_MODEL.md)); baseline capture hooks described; still relies on customer discipline to record credible baselines.  
   - **Tradeoffs:** Automated ROI claims risk skepticism; under-automation weakens expansion.  
   - **Recommendations:** Surface baseline vs measured deltas prominently post-commit (where tenant data exists); standardize a one-page “pilot scorecard” export aligned to [`docs/go-to-market/PILOT_SUCCESS_SCORECARD.md`](../go-to-market/PILOT_SUCCESS_SCORECARD.md).  
   - **Fix horizon:** v1.

5. **Differentiability — 70 / 4 / 120 / 29.41pp**  
   - **Justification:** Differentiation is real (governance + audit + replay/compare + provenance) but requires buyer education; generic “AI assistant” framing erodes edge.  
   - **Tradeoffs:** Technical depth in marketing copy increases cognitive load for economic buyers.  
   - **Recommendations:** Lead externally with **decision-grade artifacts** (manifest + immutable audit + replay) rather than model claims; keep competitor matrix refreshed against shipped scope only.  
   - **Fix horizon:** v1–v1.1.

6. **Workflow Embeddedness — 63 / 3 / 111 / 18.53pp**  
   - **Justification:** Strong API/webhooks; IDE-native workflow and flagship ITSM connectors are deferred per [`docs/library/V1_SCOPE.md`](V1_SCOPE.md) §3 — **not penalized** beyond acknowledging limits of embedding versus ServiceNow/Jira-centric shops.  
   - **Tradeoffs:** Deep integrations increase maintenance/security burden.  
   - **Recommendations:** Ship **integration recipes** (Logic Apps / Event Grid consumers) referencing [`schemas/integration-events/`](../../schemas/integration-events/) and [`docs/library/INTEGRATION_EVENTS_AND_WEBHOOKS.md`](INTEGRATION_EVENTS_AND_WEBHOOKS.md).  
   - **Fix horizon:** v1 (docs/templates); first-party connectors v1.1 per deferral register.

7. **Executive Value Visibility — 74 / 4 / 104 / 29.02pp**  
   - **Justification:** Sponsor PDF pathways and executive brief help; executives still depend on champions to translate engineering outputs.  
   - **Tradeoffs:** More executive dashboards distract from Core Pilot proof.  
   - **Recommendations:** Standardize sponsor-facing PDFs with explicit “decisions requested” sections; tie artifacts to dollars/time using conservative assumptions only ([`docs/go-to-market/ROI_MODEL.md`](../go-to-market/ROI_MODEL.md)).  
   - **Fix horizon:** v1.

8. **Usability — 70 / 3 / 90 / 20.59pp**  
   - **Justification:** Operator UI is large; progressive disclosure and authority shaping reduce accidental mutations ([`docs/library/PRODUCT_PACKAGING.md`](PRODUCT_PACKAGING.md)) but dense governance flows remain expert-friendly.  
   - **Tradeoffs:** Simplification can hide necessary controls from power users.  
   - **Recommendations:** Task-based UX audits on `/runs/new`, `/runs/[id]`, `/compare` with measurable completion metrics; expand contextual help links to [`docs/library/OPERATOR_ATLAS.md`](OPERATOR_ATLAS.md).  
   - **Fix horizon:** v1.

9. **Correctness — 78 / 4 / 88 / 30.59pp**  
   - **Justification:** Deterministic simulator paths + golden corpora + replay verification anchor correctness; LLM-backed paths introduce irreducible variance absent strict gates.  
   - **Tradeoffs:** Over-constraining LLM outputs reduces usefulness; under-constraining risks wrong manifests.  
   - **Recommendations:** Expand golden regression coverage for high-risk decision categories; ensure quality gates block commits where configured ([`docs/library/PRE_COMMIT_GOVERNANCE_GATE.md`](PRE_COMMIT_GOVERNANCE_GATE.md)).  
   - **Fix horizon:** v1.

10. **Trustworthiness — 72 / 3 / 84 / 21.18pp**  
    - **Justification:** Strong internal security narrative; external assurance artifacts (SOC 2 Type II, published pen-test summary) are **explicitly V1.1 / owner-gated** per scope — excluded from penalty but caps buyer trust at “credible self-assertion.”  
    - **Tradeoffs:** Faster assurance claims increase legal/reputational risk.  
    - **Recommendations:** Keep Trust Center brutally honest ([`docs/trust-center.md`](../trust-center.md)); attach evidence pack hashes and review dates; route procurement to questionnaire templates ([`docs/security/CAIQ_LITE_2026.md`](../security/CAIQ_LITE_2026.md)).  
    - **Fix horizon:** v1 (packaging); external attestations v1.1+.

11. **Commercial Packaging Readiness — 58 / 2 / 84 / 11.37pp**  
    - **Justification:** Quote-request capture, pricing pages, and billing webhook scaffolding exist; **live** Stripe/Marketplace publication deferred per [`docs/library/V1_SCOPE.md`](V1_SCOPE.md) — scored as packaging softness, not engineering failure.  
    - **Tradeoffs:** Self-serve accelerates revenue; premature live commerce increases support/incident risk.  
    - **Recommendations:** Keep sales-led motion documented ([`docs/go-to-market/ORDER_FORM_TEMPLATE.md`](../go-to-market/ORDER_FORM_TEMPLATE.md)); rehearse trial-to-paid migration runbooks ([`docs/runbooks/TRIAL_TO_PAID_IDENTITY_MIGRATION.md`](../runbooks/TRIAL_TO_PAID_IDENTITY_MIGRATION.md)).  
    - **Fix horizon:** v1 (motion); live commerce **owner input** (Partner Center/Stripe).

12. **Security — 76 / 3 / 72 / 22.35pp**  
    - **Justification:** Depth is strong (RLS, rate limits, billing webhook validation patterns described in STRIDE table; ZAP/Schemathesis schedules in CI). Residual concern is **configuration-dependent** real-world deployment drift and LLM-specific abuse scenarios ([`docs/security/ASK_RAG_THREAT_MODEL.md`](../security/ASK_RAG_THREAT_MODEL.md)).  
    - **Tradeoffs:** Tighter defaults can break legitimate pilots; looser defaults invite misconfiguration.  
    - **Recommendations:** Enforce “production auth mode” checks in readiness scripts beyond localhost assumptions; expand authorization boundary inventory coverage ([`docs/security/AUTHORIZATION_BOUNDARY_TEST_INVENTORY.md`](../security/AUTHORIZATION_BOUNDARY_TEST_INVENTORY.md)).  
    - **Fix horizon:** v1.

13. **Interoperability — 68 / 2 / 64 / 13.33pp**  
    - **Justification:** REST + events + Azure DevOps templates exist; lacks breadth of connector catalogs incumbents cite ([`docs/go-to-market/INTEGRATION_CATALOG.md`](../go-to-market/INTEGRATION_CATALOG.md)). Deferred connectors excluded from penalty.  
    - **Tradeoffs:** Each connector expands attack surface and CI burden.  
    - **Recommendations:** Publish **reference consumers** (minimal Azure Functions / Logic Apps) for top lifecycle events; document idempotency keys and retry semantics ([`docs/contracts/archlucid-asyncapi-2.6.yaml`](../contracts/archlucid-asyncapi-2.6.yaml)).  
    - **Fix horizon:** v1 (references).

14. **Decision Velocity — 68 / 2 / 64 / 13.33pp**  
    - **Justification:** Governance workflows deliberately add latency (SLAs, approvals); beneficial for enterprise accountability, harmful if buyer expected chat-speed answers.  
    - **Tradeoffs:** Removing friction weakens SoD; keeping friction annoys small teams.  
    - **Recommendations:** Provide **fast-path** pilot presets (policy pack severity thresholds tuned for speed) documented per tenant persona ([`docs/go-to-market/BUYER_PERSONAS.md`](../go-to-market/BUYER_PERSONAS.md)).  
    - **Fix horizon:** v1.

15. **Compliance Readiness — 68 / 2 / 64 / 13.33pp**  
    - **Justification:** Strong documentation and matrices; lacks third-party attestations buyers equate with “compliance ready.”  
    - **Tradeoffs:** Pursuing certifications early is expensive; delaying blocks some regulated buys.  
    - **Recommendations:** Maintain SOC 2 control mapping as living artifact ([`docs/security/SOC2_SELF_ASSESSMENT_2026.md`](../security/SOC2_SELF_ASSESSMENT_2026.md)); align DSAR process ([`docs/security/DSAR_PROCESS.md`](../security/DSAR_PROCESS.md)) with audit retention realities.  
    - **Fix horizon:** v1 (documentation); attestations later.

16. **Architectural Integrity — 81 / 3 / 57 / 23.82pp**  
    - **Justification:** ADRs and strangler patterns document convergence ([`docs/adr/`](../adr/README.md)); residual complexity from coordinator → authority migration notes remains specialist knowledge.  
    - **Tradeoffs:** Big-bang simplifications risk destabilizing pilots.  
    - **Recommendations:** Keep “external HTTP surface rules” enforced via architecture tests ([`ArchLucid.Architecture.Tests/`](../../ArchLucid.Architecture.Tests)); maintain parity runbooks ([`docs/runbooks/COORDINATOR_TO_AUTHORITY_PARITY.md`](../runbooks/COORDINATOR_TO_AUTHORITY_PARITY.md)).  
    - **Fix horizon:** v1.

17. **Traceability — 82 / 3 / 54 / 24.12pp**  
    - **Justification:** Explainability traces + provenance graph story are strong; completeness depends on handlers emitting traces consistently.  
    - **Tradeoffs:** Verbose traces risk PII leakage without redaction discipline.  
    - **Recommendations:** Expand trace completeness metrics/alerts as described in positioning ([`docs/go-to-market/POSITIONING.md`](../go-to-market/POSITIONING.md)); pair with [`docs/runbooks/LLM_PROMPT_REDACTION.md`](../runbooks/LLM_PROMPT_REDACTION.md).  
    - **Fix horizon:** v1.

18. **Data Consistency — 73 / 2 / 54 / 14.31pp**  
    - **Justification:** SQL transactions + RLS + optimistic concurrency patterns are credible; remaining risk is cross-channel parity (logs vs durable audit) noted internally ([`docs/library/V1_READINESS_SUMMARY.md`](V1_READINESS_SUMMARY.md)).  
    - **Tradeoffs:** Dual-writes everywhere increase latency and failure modes.  
    - **Recommendations:** Close known audit-matrix gaps where still open ([`docs/library/AUDIT_COVERAGE_MATRIX.md`](AUDIT_COVERAGE_MATRIX.md)); add reconciliation job proposals only where justified.  
    - **Fix horizon:** v1–v1.1.

19. **Reliability — 74 / 2 / 52 / 14.51pp**  
    - **Justification:** Outbox/integration patterns documented (ADR [`0004`](../adr/0004-transactional-outbox-retrieval-indexing.md)); chaos/simmy schedules exist; real-world reliability depends on Azure dependencies.  
    - **Tradeoffs:** More retries mask data bugs; fewer retries harm UX.  
    - **Recommendations:** Ensure incident runbook currency ([`docs/runbooks/INCIDENT_INVESTIGATION.md`](../runbooks/INCIDENT_INVESTIGATION.md)); codify SLO monitoring hooks ([`docs/runbooks/SLO_PROMETHEUS_GRAFANA.md`](../runbooks/SLO_PROMETHEUS_GRAFANA.md)).  
    - **Fix horizon:** v1.

20. **Procurement Readiness — 74 / 2 / 52 / 14.51pp**  
    - **Justification:** Evidence pack endpoint + templates exist ([`docs/trust-center.md`](../trust-center.md)); buyers still perform subjective review.  
    - **Tradeoffs:** Over-producing paperwork slows engineering; under-producing stalls deals.  
    - **Recommendations:** Maintain procurement index ([`docs/go-to-market/PROCUREMENT_EVIDENCE_PACK_INDEX.md`](../go-to-market/PROCUREMENT_EVIDENCE_PACK_INDEX.md)) with explicit version pins to referenced files.  
    - **Fix horizon:** v1.

21. **AI/Agent Readiness — 76 / 2 / 48 / 14.90pp**  
    - **Justification:** Simulator mode, resilience patterns, evaluation tests present; ecosystem MCP deferred ([`docs/library/MCP_AND_AGENT_ECOSYSTEM_BACKLOG.md`](MCP_AND_AGENT_ECOSYSTEM_BACKLOG.md)) — not penalized.  
    - **Tradeoffs:** Heavy evaluation infra slows iteration; light infra risks regressions.  
    - **Recommendations:** Continue nightly dataset workflows ([`.github/workflows/agent-eval-datasets-nightly.yml`](../../.github/workflows/agent-eval-datasets-nightly.yml)); document failure triage for operators ([`docs/runbooks/GOLDEN_COHORT_REAL_LLM_GATE.md`](../runbooks/GOLDEN_COHORT_REAL_LLM_GATE.md)).  
    - **Fix horizon:** v1.

22. **Explainability — 77 / 2 / 46 / 15.10pp**  
    - **Justification:** Narrative builders and traces exist; faithfulness to LLM outputs remains heuristic in places—honest limitation.  
    - **Tradeoffs:** Strong disclaimers reduce perceived magic; weak disclaimers mislead.  
    - **Recommendations:** Standardize user-visible “how this conclusion was reached” blocks on manifest and finding detail routes ([`archlucid-ui/src/app/(operator)/`](../../archlucid-ui/src/app/(operator)/)).  
    - **Fix horizon:** v1.

23. **Policy and Governance Alignment — 78 / 2 / 44 / 15.29pp**  
    - **Justification:** Policy packs + approvals + pre-commit gate align to enterprise needs; requires careful tuning to avoid blocking pilots.  
    - **Tradeoffs:** Strict policies trigger false positives; lax policies erode trust.  
    - **Recommendations:** Ship **starter packs** with rationale annotations ([`templates/policy-packs/`](../../templates/policy-packs/)); document governance dry-run mitigations ([`docs/security/GOVERNANCE_DRY_RUN_MITIGATIONS.md`](../security/GOVERNANCE_DRY_RUN_MITIGATIONS.md)).  
    - **Fix horizon:** v1.

24. **Maintainability — 79 / 2 / 42 / 15.49pp**  
    - **Justification:** Modular projects + conventions + editorconfig; large surface still demands senior engineers.  
    - **Tradeoffs:** Splitting assemblies increases coordination overhead.  
    - **Recommendations:** Keep dependency constraint tests green ([`ArchLucid.Architecture.Tests/DependencyConstraintTests.cs`](../../ArchLucid.Architecture.Tests/DependencyConstraintTests.cs)); enforce doc scope headers to reduce drift ([`scripts/ci/check_doc_scope_header.py`](../../scripts/ci/check_doc_scope_header.py)).  
    - **Fix horizon:** v1.

25. **Azure Compatibility and SaaS Deployment Readiness — 80 / 2 / 40 / 15.69pp**  
    - **Justification:** Terraform modules span edge, container apps, monitoring, SQL failover patterns; hosted probes exist ([`.github/workflows/hosted-saas-probe.yml`](../../github/workflows/hosted-saas-probe.yml)).  
    - **Tradeoffs:** Full-stack IaC complexity raises onboarding cost for small buyers self-hosting.  
    - **Recommendations:** Maintain pilot profile docs ([`docs/deployment/PILOT_PROFILE.md`](../deployment/PILOT_PROFILE.md)) aligned to Terraform roots actually used in staging/prod.  
    - **Fix horizon:** v1.

26. **Auditability — 80 / 2 / 40 / 15.69pp**  
    - **Justification:** Typed audit catalog + UI/search patterns; known gaps explicitly tracked rather than hidden.  
    - **Tradeoffs:** Auditing everything impacts performance/storage.  
    - **Recommendations:** Periodically verify CSV export expectations for large tenants; document archival interactions ([`docs/runbooks/DATA_ARCHIVAL_HEALTH.md`](../runbooks/DATA_ARCHIVAL_HEALTH.md)).  
    - **Fix horizon:** v1.

27. **Cognitive Load — 62 / 1 / 38 / 6.08pp**  
    - **Justification:** Rich feature set + governance vocabulary increases mental burden despite disclosure tiers.  
    - **Tradeoffs:** Oversimplification undermines Operate value props.  
    - **Recommendations:** Add progressive onboarding checklist component on home; reduce simultaneous concepts per screen on `/runs/new`.  
    - **Fix horizon:** v1.

28. **Stickiness — 65 / 1 / 35 / 6.37pp**  
    - **Justification:** Manifest + audit trail improves switching costs; sticky workflows depend on embedding ArchLucid into recurring ARB cadence.  
    - **Tradeoffs:** Stickiness via lock-in narratives can alarm procurement.  
    - **Recommendations:** Emphasize portable exports (ZIP/DOCX/OpenAPI contracts) to reduce perceived trap dynamics while retaining audit advantage.  
    - **Fix horizon:** v1.

29. **Scalability — 68 / 1 / 32 / 6.67pp**  
    - **Justification:** Single-region V1 posture honestly documented; k6 evidence merge-blocking but not universal proof ([`docs/library/LOAD_TEST_BASELINE.md`](LOAD_TEST_BASELINE.md)).  
    - **Tradeoffs:** Promising scale invites obligations ArchLucid declines today.  
    - **Recommendations:** Keep [`docs/library/BUYER_SCALABILITY_FAQ.md`](BUYER_SCALABILITY_FAQ.md) tightly coupled to tests/workflows cited; avoid marketing SLAs.  
    - **Fix horizon:** v1.

30. **Cost-Effectiveness — 70 / 1 / 30 / 6.86pp**  
    - **Justification:** LLM spend is inherently spikey; internal models/docs exist ([`docs/library/CAPACITY_AND_COST_PLAYBOOK.md`](CAPACITY_AND_COST_PLAYBOOK.md)).  
    - **Tradeoffs:** Aggressive caching harms freshness of answers in Ask surfaces.  
    - **Recommendations:** Surface per-tenant estimates where already computed ([`docs/deployment/PER_TENANT_COST_MODEL.md`](../deployment/PER_TENANT_COST_MODEL.md)); add operator-visible token guardrails dashboards if not already complete in UI.  
    - **Fix horizon:** v1–v1.1.

31. **Change Impact Clarity — 70 / 1 / 30 / 6.86pp**  
    - **Justification:** `BREAKING_CHANGES.md`, API versioning docs help; dense changelog requires curator.  
    - **Tradeoffs:** Too much process slows releases; too little surprises integrators.  
    - **Recommendations:** Ensure OpenAPI snapshot contracts remain CI-enforced ([`ArchLucid.Api.Tests/Contracts/openapi-v1.contract.snapshot.json`](../../ArchLucid.Api.Tests/Contracts/openapi-v1.contract.snapshot.json)).  
    - **Fix horizon:** v1.

32. **Template and Accelerator Richness — 72 / 1 / 28 / 7.06pp**  
    - **Justification:** Templates for integrations/policy packs/finding engines exist under [`templates/`](../../templates/README.md).  
    - **Tradeoffs:** Templates rot without CI smoke.  
    - **Recommendations:** Add lightweight CI validation where missing for template README claims (without introducing heavy new deps).  
    - **Fix horizon:** v1.

33. **Availability — 72 / 1 / 28 / 7.06pp**  
    - **Justification:** Health endpoints + runbooks; no active/active guarantee in V1 contract.  
    - **Tradeoffs:** HA features cost money/complexity.  
    - **Recommendations:** Document RTO/RPO targets as planning-only ([`docs/library/RTO_RPO_TARGETS.md`](RTO_RPO_TARGETS.md)); rehearse failover drill ([`docs/runbooks/GEO_FAILOVER_DRILL.md`](../runbooks/GEO_FAILOVER_DRILL.md)).  
    - **Fix horizon:** v1 ops playbooks; topology v1.1+.

34. **Accessibility — 72 / 1 / 28 / 7.06pp**  
    - **Justification:** Axe component suite + live accessibility specs exist under [`archlucid-ui/e2e/`](../../archlucid-ui/e2e); dense data tables remain challenging.  
    - **Tradeoffs:** Full WCAG remediation competes with feature throughput.  
    - **Recommendations:** Expand jest-axe coverage to highest-traffic operator components; track failures like other merge gates.  
    - **Fix horizon:** v1.

35. **Extensibility — 73 / 1 / 27 / 7.16pp**  
    - **Justification:** Finding engines/templates/extension points exist; extension ergonomics still require reading many docs.  
    - **Tradeoffs:** Formal plugin APIs increase compatibility commitments.  
    - **Recommendations:** Maintain “add a comparison type” / “add integration recipe” guides ([`docs/HOWTO_ADD_COMPARISON_TYPE.md`](../HOWTO_ADD_COMPARISON_TYPE.md), [`docs/integrations/recipes/README.md`](../integrations/recipes/README.md)).  
    - **Fix horizon:** v1.

36. **Performance — 74 / 1 / 26 / 7.25pp**  
    - **Justification:** BenchmarkDotNet / k6 thresholds exist; performance not universally benchmarked per route.  
    - **Tradeoffs:** Micro-benchmarking everything is expensive.  
    - **Recommendations:** Expand hot-path profiling notes in [`docs/library/PERFORMANCE_BASELINES.md`](PERFORMANCE_BASELINES.md) when schema shifts occur (recent page compression migrations imply attention).  
    - **Fix horizon:** v1.

37. **Evolvability — 74 / 1 / 26 / 7.25pp**  
    - **Justification:** DbUp migrations + ADRs support evolution; migration count is high—requires discipline.  
    - **Tradeoffs:** Rapid schema churn burdens hosted tenants.  
    - **Recommendations:** Keep consolidated DDL [`ArchLucid.Persistence/Scripts/ArchLucid.sql`](../../ArchLucid.Persistence/Scripts/ArchLucid.sql) aligned per house rules; rehearse rollback posture ([`docs/runbooks/MIGRATION_ROLLBACK.md`](../runbooks/MIGRATION_ROLLBACK.md)).  
    - **Fix horizon:** v1.

38. **Manageability — 76 / 1 / 24 / 7.45pp**  
    - **Justification:** Feature gates + extensive configuration docs; misconfiguration remains the dominant failure mode in SaaS.  
    - **Tradeoffs:** Strong guardrails can block demos.  
    - **Recommendations:** Expand `doctor` checks for dangerous prod configs ([`ArchLucid.Cli`](../../ArchLucid.Cli)); surface warnings in [`ArchLucid.Api` startup](../../ArchLucid.Api/Program.cs) patterns already used for auth modes.  
    - **Fix horizon:** v1.

39. **Customer Self-Sufficiency — 76 / 1 / 24 / 7.45pp**  
    - **Justification:** Documentation depth is exceptional; sheer volume can overwhelm.  
    - **Tradeoffs:** Concise docs omit edge cases; exhaustive docs intimidate.  
    - **Recommendations:** Maintain [`docs/START_HERE.md`](../START_HERE.md) as strict funnel; add “if you only read three pages” card in operator UI help tray.  
    - **Fix horizon:** v1.

40. **Observability — 77 / 1 / 23 / 7.55pp**  
    - **Justification:** Runbooks for traces/metrics exist; completeness varies by subsystem.  
    - **Tradeoffs:** Verbose telemetry increases cost and privacy review burden.  
    - **Recommendations:** Align OTel collector Terraform with documented dashboards ([`infra/terraform-otel-collector/`](../../infra/terraform-otel-collector/README.md)); validate correlation ID propagation through worker paths ([`docs/runbooks/AUTHORITY_PIPELINE_OBSERVABILITY.md`](../runbooks/AUTHORITY_PIPELINE_OBSERVABILITY.md)).  
    - **Fix horizon:** v1.

41. **Deployability — 78 / 1 / 22 / 7.65pp**  
    - **Justification:** Docker Compose profiles + Terraform footprint + CD workflows ([`.github/workflows/cd-staging-on-merge.yml`](../../.github/workflows/cd-staging-on-merge.yml)).  
    - **Tradeoffs:** Multiple infra roots increase choice paralysis.  
    - **Recommendations:** Publish a **single** recommended staging blueprint linking modules in [`infra/README.md`](../../infra/README.md) without duplicating terraform sources.  
    - **Fix horizon:** v1.

42. **Azure Ecosystem Fit — 81 / 1 / 19 / 7.94pp**  
    - **Justification:** Entra, Container Apps, SQL, Service Bus, Marketplace alignment documented (ADR [`0020`](../adr/0020-azure-primary-platform-permanent.md)).  
    - **Tradeoffs:** Multi-cloud buyers feel excluded—accepted ICP tradeoff.  
    - **Recommendations:** Where AWS/GCP mentions appear, frame explicitly as out-of-scope for V1 unless connector roadmap opens ([`docs/library/V1_SCOPE.md`](V1_SCOPE.md)).  
    - **Fix horizon:** positioning/docs v1.

43. **Supportability — 82 / 1 / 18 / 8.04pp**  
    - **Justification:** Support bundle + troubleshooting + version endpoints are mature patterns.  
    - **Tradeoffs:** Bundles may contain sensitive snippets—requires hygiene guidance.  
    - **Recommendations:** Ensure CLI bundle redaction notes remain prominent ([`docs/library/CLI_USAGE.md`](CLI_USAGE.md)).  
    - **Fix horizon:** v1.

44. **Modularity — 84 / 1 / 16 / 8.24pp**  
    - **Justification:** Clear assemblies (`ArchLucid.Application`, `Decisioning`, `Persistence.*`, UI separation).  
    - **Tradeoffs:** Many projects increase build graph time / cognitive overhead for newcomers.  
    - **Recommendations:** Keep architecture tests enforcing dependency direction; document “where to change X” ([`docs/ARCHITECTURE_COMPONENTS.md`](ARCHITECTURE_COMPONENTS.md)).  
    - **Fix horizon:** v1.

45. **Testability — 85 / 1 / 15 / 8.38pp**  
    - **Justification:** Very strong automated pyramid; live E2E breadth is unusual for this maturity.  
    - **Tradeoffs:** CI duration/cost; flaky browser tests require nursing.  
    - **Recommendations:** Keep mock vs live separation disciplined ([`docs/library/RELEASE_SMOKE.md`](RELEASE_SMOKE.md)); extend selective quarantine tooling only with documented ownership.  
    - **Fix horizon:** v1.

46. **Documentation — 88 / 1 / 12 / 8.63pp**  
    - **Justification:** Dense, navigated spine (`START_HERE`, `ARCHITECTURE_INDEX`, library inventory patterns); CI guards doc hygiene.  
    - **Tradeoffs:** Documentation debt manifests as contradictions—requires ongoing curation.  
    - **Recommendations:** Continue enforcing doc scope headers + root markdown budget scripts; periodically prune stale cross-links via navigator checker ([`.github/workflows/ci.yml`](../../.github/workflows/ci.yml)).  
    - **Fix horizon:** v1.

---

## Top 10 Most Important Weaknesses

1. **Category / narrative collision** — Buyers bucket ArchLucid next to chat assistants or EAM suites unless educated; differentiation requires proof artifacts, not slogans.  
2. **Broad operator surface vs pilot wedge** — Progressive disclosure helps, but the product still “looks enterprise-heavy” on first login, raising perceived adoption cost.  
3. **Assurance asymmetry** — Engineering controls are strong; **external** attestations lag buyer expectations in regulated procurement.  
4. **Integration reality vs incumbent comparisons** — Deferred ITSM connectors are honest, but evaluation matrices still invite “where’s Jira/ServiceNow native?” friction (address with recipes + roadmap pointers).  
5. **LLM correctness & explainability limits** — Buyers may interpret outputs as authoritative engineering sign-off without operational discipline.  
6. **Commercial motion still sales-led at V1 boundary** — Appropriate for risk management; limits velocity of PLG revenue expansion.  
7. **Single-region scalability posture** — Correctly documented; still triggers enterprise DR questionnaires.  
8. **Dual-channel auditing/logging parity complexity** — Buyers chasing “immutable proof for everything” can find gaps if marketing oversells.  
9. **Time-to-value variability** — Depends on IdP, tenant defaults, and operator skill; product can reduce variance further with guided scripts.  
10. **Cognitive load for Part-Time Operators** — Champions may not be full-time ArchLucid admins; UI density risks shelf-ware dynamics.

---

## Top 5 Monetization Blockers

1. **Buyer uncertainty whether AI output is decision-grade** without internal champion translation → slows expansion POs.  
2. **Category noise (“another AI tool”)** weakens willingness-to-pay absent a sharp procurement story.  
3. **Sales-led commerce boundary** (live Stripe/Marketplace publication deferred as explicit scope) caps frictionless conversion for segments expecting instant pay-as-you-go enterprise procurement.  
4. **ICP narrowness (Azure-primary, established EA practice)** excludes large adjacent budgets ([`docs/go-to-market/IDEAL_CUSTOMER_PROFILE.md`](../go-to-market/IDEAL_CUSTOMER_PROFILE.md)).  
5. **Sticky expansion requires workflow embedding** — Teams budget renewal when ArchLucid sits inside ARB/ITSM cadence; webhook/API path works but needs proven templates.

---

## Top 5 Enterprise Adoption Blockers

1. **Absent SOC 2 Type II / comparable third-party report** for default enterprise procurement (“trust via paperwork”).  
2. **Penetration test artifacts not yet in public Trust Center posture** (explicit V1.1 / owner gate — message honestly or deals stall in security review).  
3. **DR/multi-region expectations** vs documented single-region V1 contract ([`docs/library/BUYER_SCALABILITY_FAQ.md`](BUYER_SCALABILITY_FAQ.md)).  
4. **LLM data handling & prompt retention questions** — Must be answered with cited controls ([`docs/runbooks/LLM_PROMPT_REDACTION.md`](../runbooks/LLM_PROMPT_REDACTION.md), threat notes).  
5. **Segregation-of-duties + governance setup burden** — Buyers without mature process may blame product for “slowing them down.”

---

## Top 5 Engineering Risks

1. **Misconfiguration of auth/CORS/storage in customer environments** leading to accidental exposure or lockout (high impact, config-driven).  
2. **LLM abuse paths** (prompt injection → unsafe downstream actions) despite mitigations—requires continuous red-team mindset ([`docs/security/ASK_RAG_THREAT_MODEL.md`](../security/ASK_RAG_THREAT_MODEL.md)).  
3. **Data correctness under partial failures** in asynchronous pipelines (worker/outbox delays; operator-visible inconsistency windows).  
4. **Schema evolution / migration failures** on long-lived tenants if operational discipline slips ([`docs/engineering/BUILD.md`](../engineering/BUILD.md)).  
5. **CI/flaky test drift** if live Playwright suite grows without ownership—risk of false confidence or slowing merges.

---

## Most Important Truth

**ArchLucid’s readiness is engineering-forward:** it already behaves like a disciplined platform (tests, IaC, audit intent, docs), but **enterprise buyers pay for certainty**—and certainty today still depends heavily on **your deployment discipline, pilot framing, and honest assurance posture**, not on the feature list alone.

---

## Top Improvement Opportunities

Ranked by leverage. **One item is DEFERRED** (owner-only gates); **eight additional actionable prompts** follow so there are **8 non-deferred** complete Cursor prompts.

### 1. Pilot-first onboarding shrink-wrap (Core Pilot friction)

- **Why it matters:** Adoption friction is the largest weighted gap driver; narrowing the first session increases conversion and measurable time-to-first-manifest.  
- **Expected impact:** Medium-high reduction in early drop-off; improves Adoption Friction (+8–12 pts), Time-to-Value (+4–7 pts), Cognitive Load (+5–8 pts). **Weighted readiness impact:** ~+1.0–1.5%.  
- **Affected qualities:** Adoption Friction, Time-to-Value, Cognitive Load, Usability.  
- **Actionable now:** Yes.

**Cursor prompt (complete)**

```text
You are working in the ArchLucid repo. Goal: reduce first-session friction for the Core Pilot path without changing API authorization semantics.

Scope:
- UI only (archlucid-ui), plus minimal doc links updates under docs/ if needed.
- Keep API policies and /api/auth/me behavior unchanged; UI remains shaping-only.

Tasks:
1) On the operator Home / getting-started surfaces, add a prominent “Core Pilot in one session” checklist that maps 1:1 to docs/CORE_PILOT.md (Create request → Execute → Commit → Review). Each step deep-links to the correct route (/runs/new, run detail, etc.).
2) Default-collapse or de-emphasize Operate discovery promos until at least one successful commit exists for the tenant (use existing client-side signals already available in the UI; do not add new API endpoints unless absolutely necessary).
3) Add concise empty-state copy on /runs when no runs exist that explicitly tells users to ignore Compare/Replay/Governance until after first commit.
4) Add/adjust Vitest tests for the checklist presence and gating logic; do not use ConfigureAwait(false) in any C# tests (N/A here).

Acceptance criteria:
- New operators see Core Pilot checklist above the fold on the landing route you choose (justify choice in PR description).
- No route authorization changes; hidden links remain reachable by URL but copy discourages early use.
- Tests green: npm test in archlucid-ui.

Constraints:
- Do not edit historical SQL migrations.
- Do not introduce marketing claims beyond docs/CORE_PILOT.md wording.

Non-goals:
- Billing/Stripe/Marketplace changes.
- Terraform changes.

Impact note for reviewers: primarily Adoption Friction / Time-to-Value / Cognitive Load.
```

---

### 2. Buyer-proof“decision-grade artifacts” messaging alignment

- **Why it matters:** Marketability + Differentiability hinge on proof, not adjectives.  
- **Expected impact:** Marketability (+4–7 pts), Differentiability (+5–8 pts), Executive Value Visibility (+3–5 pts). **Weighted readiness:** ~+0.7–1.1%.  
- **Affected qualities:** Marketability, Differentiability, Executive Value Visibility.  
- **Actionable now:** Yes.

**Cursor prompt (complete)**

```text
Repo: ArchLucid. Goal: tighten outward messaging so every flagship marketing/why/showcase claim citations a shipped artifact path (API route, UI route, or export) consistent with docs/V1_SCOPE.md.

Tasks:
1) Audit archlucid-ui marketing pages under archlucid-ui/src/app/(marketing)/ for claims without inline “where to verify” pointers.
2) For each claim, either add a concise “Verify” link (internal route or public demo route) OR soften language to match V1_SCOPE boundaries.
3) Update docs/go-to-market/POSITIONING.md only if necessary to remove mismatches discovered during audit (keep pricing numbers out unless already allowed by pricing single-source CI rule—link to PRICING_PHILOSOPHY.md instead).
4) Add a short docs/library/ stub ONLY if required by docs root budget rules—prefer updating existing docs under docs/go-to-market/.

Acceptance criteria:
- No new pricing literals outside allowed files (scripts/ci/check_pricing_single_source.py).
- Marketing claims align with V1_SCOPE.md; deferred items explicitly labeled “V1.1+” where mentioned.
- UI builds: npm run build.

Constraints:
- Do not reference prior assessments.
- Do not modify Terraform.

Impact note: strengthens Marketability/Differentiability without engineering risk.
```

---

### 3. Integration recipes v1 (webhooks → tickets)

- **Why it matters:** Workflow Embeddedness + Interoperability remain buyer friction without deferred connectors.  
- **Expected impact:** Interoperability (+6–10 pts), Workflow Embeddedness (+5–8 pts), Adoption Friction (+3–5 pts). **Weighted readiness:** ~+0.8–1.2%.  
- **Affected qualities:** Interoperability, Workflow Embeddedness, Adoption Friction.  
- **Actionable now:** Yes.

**Cursor prompt (complete)**

```text
Goal: Add two copy-paste integration recipes that consume CloudEvents-style integration events without promising first-party Jira/ServiceNow connectors.

Tasks:
1) Under docs/integrations/recipes/, add:
   - recipe-azure-logic-apps-webhook-to-ado-work-item.md
   - recipe-event-grid-webhook-hardening-checklist.md
2) Each recipe must include: authentication choices (hmac/shared secret pattern consistent with docs/library/INTEGRATION_EVENTS_AND_WEBHOOKS.md), idempotency guidance, example payload pointers under schemas/integration-events/, failure modes, and explicit “V1 scope” boundary statement referencing docs/library/V1_SCOPE.md §3 ITSM rows.
3) Link recipes from docs/go-to-market/INTEGRATION_CATALOG.md §2 as “V1 supported patterns” without bumping planned connectors’ status incorrectly.

Acceptance criteria:
- First line scope header matches CI rule for docs.
- Links resolve (navigator checker scripts if applicable).
- No new HTTP routes required.

Constraints:
- Do not implement new connectors in ArchLucid.Api.
- Do not edit historical SQL migrations.

Impact note: improves Interoperability without expanding attack surface materially.
```

---

### 4. ROI scorecard export parity

- **Why it matters:** Proof-of-ROI Readiness is weighted highly; operational teams forget baselines unless prompted in-product.  
- **Expected impact:** Proof-of-ROI Readiness (+6–9 pts), Executive Value Visibility (+3–6 pts). **Weighted readiness:** ~+0.5–0.9%.  
- **Affected qualities:** Proof-of-ROI, Executive Value Visibility, Stickiness.  
- **Actionable now:** Yes (partially — if tenant signals missing, degrade gracefully).

**Cursor prompt (complete)**

```text
Goal: Make pilot ROI artifacts easier to generate post-commit without inventing new economics formulas.

Tasks:
1) Locate existing value report / first-value report flows (API + UI). Ensure the operator run detail page surfaces a single obvious primary CTA after commit: “Generate pilot scorecard package” linking to existing exports (Markdown/PDF/DOCX) WITHOUT duplicating logic.
2) If multiple endpoints overlap, consolidate UI wording to point at the canonical sponsor narrative described in docs/EXECUTIVE_SPONSOR_BRIEF.md and measurement model in docs/library/PILOT_ROI_MODEL.md.
3) Add UI tests (Vitest or Playwright mock suite) asserting CTA visibility rules: appears only after successful commit; hidden on failed/incomplete runs.

Acceptance criteria:
- No new pricing literals.
- API remains authoritative for entitlements; UI does not fake access.
- Tests green.

Constraints:
- Do not change billing providers.
- Do not add ConfigureAwait(false) in tests if touching C#.

Impact note: improves Proof-of-ROI Readiness with low risk.
```

---

### 5. Audit coverage matrix closure drill (documentation-driven)

- **Why it matters:** Data Consistency + Trustworthiness + Compliance narratives improve when gaps are explicit and shrinking.  
- **Expected impact:** Auditability (+3–6 pts), Data Consistency (+4–7 pts), Trustworthiness (+3–5 pts). **Weighted readiness:** ~+0.4–0.8%.  
- **Affected qualities:** Auditability, Data Consistency, Trustworthiness.  
- **Actionable now:** Yes (start with audit + small targeted emits if gaps are real and low-risk).

**Cursor prompt (complete)**

```text
Goal: Reduce buyer-facing ambiguity about durable audit coverage without claiming completeness falsely.

Tasks:
1) Read docs/library/AUDIT_COVERAGE_MATRIX.md Known gaps section. For each open gap, either:
   (A) confirm already resolved in code and update matrix + link to test names, OR
   (B) implement minimal durable audit emission for that mutation path (ArchLucid.Api + IAuditService patterns), with focused integration tests in ArchLucid.Api.Tests.
2) Update docs/library/V1_READINESS_SUMMARY.md only if it currently contradicts the matrix after fixes.

Acceptance criteria:
- dotnet test ArchLucid.sln --filter "Suite=Core&Category!=Slow&Category!=Integration" passes locally.
- Any new audit events extend AUDIT_COVERAGE_MATRIX.md.

Constraints:
- Never modify historical migration files 001–028; if schema needed, add new forward migration + update ArchLucid.sql master DDL per repo rules.

Impact note: strengthens trust chain for enterprise reviewers.
```

---

### 6. Security operational guardrails: dangerous production config warnings

- **Why it matters:** Security score constrained by deployment realism; proactive warnings reduce incidents.  
- **Expected impact:** Security (+4–7 pts), Manageability (+4–6 pts), Reliability (+2–4 pts). **Weighted readiness:** ~+0.4–0.7%.  
- **Affected qualities:** Security, Manageability, Reliability.  
- **Actionable now:** Yes.

**Cursor prompt (complete)**

```text
Goal: Make misconfiguration harder in production-like environments.

Tasks:
1) In ArchLucid.Api startup, add guarded warnings (logs + optional ProblemDetails only on admin diagnostics endpoint if already exists—prefer logs to avoid info leaks) when:
   - Cors:AllowedOrigins is empty while deployment profile suggests staging/production hosting
   - Authentication schemes imply insecure combinations (document the rules in docs/engineering/BUILD.md)
2) Extend ArchLucid.Cli doctor to print the same warnings client-side when reachable.
3) Add ArchLucid.Api.Tests covering warning emission using WebApplicationFactory patterns already in repo.

Acceptance criteria:
- DevelopmentBypass remains possible in Development only; never silently weaken prod security.
- Tests follow repo style rules (null checks with `is null`; avoid trailing else after return).

Constraints:
- Do not change default shipped security posture except logging visibility.

Impact note: reduces real-world security incidents from ops mistakes.
```

---

### 7. Performance regression sentinel for hot listing endpoints

- **Why it matters:** Performance weight is small but operational incidents erode trust; cheap guards help.  
- **Expected impact:** Performance (+5–8 pts), Reliability (+3–5 pts), Cost-Effectiveness (+2–4 pts). **Weighted readiness:** ~+0.2–0.4%.  
- **Affected qualities:** Performance, Reliability, Cost-Effectiveness.  
- **Actionable now:** Yes.

**Cursor prompt (complete)**

```text
Goal: Add a lightweight regression test protecting SQL shapes for hot list endpoints (runs list / audit search), without full perf lab.

Tasks:
1) Identify primary hot-path queries for runs listing and audit paging (ArchLucid.Persistence + ArchLucid.Api).
2) Add ArchLucid.Persistence.Tests or ArchLucid.Api.Tests that assert generated SQL contains expected key predicates/index-friendly patterns OR uses approved stored procedures—pick the approach already used elsewhere in tests.
3) Update docs/library/PERFORMANCE_BASELINES.md with a short note explaining the sentinel.

Acceptance criteria:
- Tests are deterministic; no shared production DB.
- No historical migration edits.

Impact note: prevents accidental ORM-like regressions (even though Dapper is used).
```

---

### 8. DEFERRED — Production commerce unlock (Stripe live + Marketplace Published + signup DNS)

- **Why it matters:** Largest commercial velocity unlock; cannot be completed without owner-controlled external dependencies.  
- **Expected impact:** Commercial Packaging Readiness (+15–25 pts), Decision Velocity (+5–10 pts marketing ops), Marketability (+3–6 pts). **Weighted readiness:** could move multiple points once executed.  
- **Affected qualities:** Commercial Packaging Readiness, Marketability, Decision Velocity.  
- **Actionable now:** **DEFERRED**

**DEFERRED details (no full Cursor prompt per instructions)**

- **Title:** DEFERRED — Production commerce unlock (Stripe live + Marketplace Published + signup DNS cutover)  
- **Reason deferred:** Explicit V1 scope boundary + owner-only Partner Center/Tax/Payout verification steps ([`docs/library/V1_SCOPE.md`](V1_SCOPE.md) §3 commerce row).  
- **Information needed later:** Go/no-go date; Stripe live mode approval; Marketplace offer state; DNS ownership for signup host; support escalation owner roster.

---

### 9. Explainability UX uniform footer on manifest + finding surfaces

- **Why it matters:** Correctness/Trustworthiness perceptions improve when every artifact reminds users what evidence supports conclusions.  
- **Expected impact:** Explainability (+5–8 pts), Trustworthiness (+4–6 pts), Cognitive Load (+3–5 pts via consistency). **Weighted readiness:** ~+0.3–0.6%.  
- **Affected qualities:** Explainability, Trustworthiness, Cognitive Load.  
- **Actionable now:** Yes (ensures 8 non-deferred actionable prompts total).

**Cursor prompt (complete)**

```text
Goal: Add a consistent “Evidence & limits” footer component to operator manifest summary and finding detail views.

Tasks:
1) Create a small React component under archlucid-ui/src/components/ that renders:
   - links to existing explain/provenance routes where applicable
   - concise disclaimer: simulator vs live execution provenance text sourced from existing API fields (do not invent backend behavior)
2) Wire into pages under archlucid-ui/src/app/(operator)/runs/ and findings routes.
3) Add Vitest tests asserting footer renders when API payloads include execution metadata flags; omit when unknown.

Acceptance criteria:
- No API changes unless strictly necessary; prefer consuming existing DTO fields from proxy layer.
- Accessibility: include readable text, not hover-only tooltips.

Constraints:
- Do not change LLM prompts here.

Impact note: boosts Explainability/Trustworthiness with low blast radius.
```

---

## Pending Questions for Later

Organized by improvement title; **blocking / decision-shaping only**.

1. **Pilot-first onboarding shrink-wrap** — Should Operate links be fully hidden vs disabled until first commit for **all** tenants, or only Trial/Team tiers?  
2. **Buyer-proof messaging alignment** — Which demo hostnames are canonical for external screenshots to avoid `*.example.com` drift?  
3. **Integration recipes** — Preferred reference automation platform for customers: Logic Apps vs Functions vs Event Grid-only?  
4. **ROI scorecard export parity** — Which single export is “canonical sponsor artifact” if endpoints disagree (PDF vs DOCX vs Markdown)?  
5. **Audit coverage matrix closure** — Policy preference: emit audit on best-effort async paths vs fail requests when audit write fails?  
6. **Production config warnings** — Should warnings escalate to OpenTelemetry metrics (requires cardinality review)?  
7. **Performance regression sentinel** — Allowlist approach vs snapshot SQL text — which is more acceptable to DBAs on your target accounts?  
8. **DEFERRED commerce unlock** — Target segment for PLG vs sales-led (mid-market vs enterprise) once live commerce returns.  
9. **Explainability UX footer** — Legal/compliance approval needed for disclaimer wording in regulated industries?

---

**End of assessment artifact.**
