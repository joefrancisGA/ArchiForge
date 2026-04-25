# ArchLucid Assessment – Weighted Readiness 65.99%

**Date:** 2026-04-24
**Assessor:** Independent first-principles review from codebase materials
**Method:** Scored 46 qualities (1–100), weighted per provided model, ordered by weighted deficiency

---

## 1. Executive Summary

### Overall Readiness

ArchLucid is a deeply engineered AI-assisted architecture workflow platform with exceptional internal structure and comprehensive documentation. At 65.99% weighted readiness, the product has strong engineering bones but significant commercial and trust gaps that will prevent revenue until addressed. The architecture is sound, the code is modular, and the documentation is unusually thorough for a pre-revenue product — but no paying customer has validated the value proposition, no third-party security attestation exists, and the product's complexity works against the rapid adoption it needs.

### Commercial Picture

The commercial machinery is well-designed on paper — three-tier pricing, ROI model, buyer journey, trial design, procurement pack — but it is untested. Zero reference customers, zero published case studies, and an ROI worked example with placeholder data. The pricing philosophy is thoughtful ($294K annual value capture at 10–20%) but unvalidated. The self-serve billing loop is not fully in production. Marketability is the single largest weighted deficiency in the entire assessment (384 weighted points). The product solves a real problem, but the "AI architecture OS" category does not yet exist in buyer vocabulary, and the 30-second elevator pitch remains elusive.

### Enterprise Picture

Enterprise readiness is bifurcated. Internally-facing controls are strong: append-only audit trail, governance approval workflows with segregation of duties, provenance graphs, RLS tenant isolation, policy packs. Externally-facing trust evidence is weak: SOC 2 is self-assessed only (no CPA attestation), the pen test is in flight but not completed, and no enterprise buyer has been through the procurement process. ITSM integrations (Jira, ServiceNow, Confluence) are explicitly deferred to V1.1 — a legitimate scope decision but a meaningful adoption friction point for enterprises with established toolchains.

### Engineering Picture

Engineering quality is the strongest of the three pillars. The 53-project solution is cleanly decomposed with architecture tests enforcing boundaries, ADR-driven decisions, and a mature CI pipeline with OpenAPI contract testing, Schemathesis fuzzing, OWASP ZAP, and code coverage gates. The agent simulator enables deterministic testing without LLM cost. Observability is rich (30+ custom Prometheus instruments). Key weaknesses: code coverage (73% line) sits below the strict 79% gate, the Persistence layer is notably under-covered at 40%, and no production workload has validated reliability, performance, or scale claims.

---

## 2. Weighted Quality Assessment

Qualities ordered from most urgent (highest weighted deficiency) to least urgent.

---

### 1. Marketability
| Attribute | Value |
|-----------|-------|
| **Score** | 52 |
| **Weight** | 8 |
| **Weighted Deficiency** | 384 |

**Justification:** ArchLucid addresses a genuine pain point — manual architecture review packaging — but the "AI-assisted architecture workflow" category is undefined in buyer vocabulary. There are zero named customers, zero published case studies, zero logos. The product name is strong and the documentation is extensive, but the positioning relies on the buyer already knowing they have this problem. The ROI worked example contains placeholder data (`(pending) | Run scripts/ops/generate-worked-example-roi.ps1`). Marketing surfaces exist (demo preview endpoint, pricing page, trust center) but are untested with real prospects. The 50% discount stack (trust -25%, reference -15%, self-serve -10%) honestly reflects the market maturity gap.

**Tradeoffs:** Extensive documentation and go-to-market planning versus zero market validation. The product could be over-engineered for a market that does not yet exist in its imagined form, or it could be perfectly positioned for an emergent category.

**Improvement Recommendations:**
- Generate the worked example ROI artifact from the existing demo seed to replace placeholder data
- Create a 30-second positioning statement that avoids category jargon
- Pursue first design-partner customer to unlock the reference discount gate
- Run the self-serve trial loop end-to-end in a staging environment with real prospect flow

---

### 2. Adoption Friction
| Attribute | Value |
|-----------|-------|
| **Score** | 55 |
| **Weight** | 6 |
| **Weighted Deficiency** | 270 |

**Justification:** Azure-only deployment (Entra ID, Azure SQL, Container Apps) limits the addressable market to organizations already invested in Azure. The multi-stakeholder sales motion (EA, security reviewer, SRE, procurement) adds cycle time. Trial design is sound (14 days, <5 min to active) but the self-serve billing loop is not fully operational. No ITSM integrations in V1 means enterprises with Jira/ServiceNow workflows must build their own bridges via webhooks. The product surface is vast (90+ API controllers, 53 projects, 455 docs) which can overwhelm evaluators.

**Tradeoffs:** Azure-native commitment provides security/compliance coherence but narrows the market. The V1.1 deferral of Jira/ServiceNow is a legitimate scope decision but raises early-deal friction.

**Improvement Recommendations:**
- Prioritize the self-serve trial-to-paid conversion loop as the single most important adoption accelerator
- Create a "5-minute evaluator path" that demonstrates core value without requiring Azure infrastructure
- Document webhook-to-Jira bridge pattern as a V1 workaround until V1.1 connectors ship
- Reduce the documentation surface visible to first-time evaluators

---

### 3. Time-to-Value
| Attribute | Value |
|-----------|-------|
| **Score** | 62 |
| **Weight** | 7 |
| **Weighted Deficiency** | 266 |

**Justification:** Docker demo and sample presets enable a fast first impression. The `archlucid try --real` path exists for real LLM evaluation. First-run wizard with pre-loaded sample run is well-designed. The pilot ROI model and first-value-report PDF endpoint (`POST /v1/pilots/runs/{runId}/first-value-report.pdf`) create tangible sponsor-shareable outputs. However: the trial loop is not fully in production, real LLM mode adds cost (~$1.25 per pipeline), and the time from "interested" to "first committed manifest with my own architecture request" is non-trivial — it requires understanding the architecture request schema, agent types, and manifest concepts.

**Tradeoffs:** Simulator mode enables zero-cost evaluation but delivers synthetic outputs. Real mode delivers authentic value but adds cost and Azure OpenAI configuration overhead.

**Improvement Recommendations:**
- Pre-populate three industry-specific architecture request templates so evaluators skip the "blank page" problem
- Ensure the demo preview endpoint delivers a compelling first impression at `archlucid.com/demo/preview`
- Reduce the steps between "sign up" and "see my first committed manifest" to under 10 minutes

---

### 4. Proof-of-ROI Readiness
| Attribute | Value |
|-----------|-------|
| **Score** | 58 |
| **Weight** | 5 |
| **Weighted Deficiency** | 210 |

**Justification:** The ROI framework is well-constructed: $294K annual savings for a 6-architect team, break-even at ~180 architect-hours/year, pricing at 10–20% of value delivered. The `pilot-run-deltas` endpoint returns structured proof data (time-to-committed-manifest, findings counts, audit counts, LLM call counts). First-value-report PDF is sponsor-shareable. However, the worked example ROI document contains only placeholder data. No real customer has validated the time savings or the ROI model. The baseline review-cycle capture (soft-required at signup) is a smart design but unvalidated.

**Tradeoffs:** The ROI model is grounded in reasonable assumptions about manual architecture review costs, but the leap from "this is plausible" to "this is proven" requires at least one customer's data.

**Improvement Recommendations:**
- Run `scripts/ops/generate-worked-example-roi.ps1` against the demo seed to populate the worked example with concrete sample numbers
- Add a simulated "before/after" comparison to the demo preview that shows time-to-manifest delta
- Instrument the pilot-run-deltas endpoint in the trial flow so ROI data accumulates automatically

---

### 5. Differentiability
| Attribute | Value |
|-----------|-------|
| **Score** | 60 |
| **Weight** | 4 |
| **Weighted Deficiency** | 160 |

**Justification:** ArchLucid's honest competitor contrast acknowledges where it does and does not win. Unique strengths: versioned manifests tied to runs, replay/compare, LLM-assisted structuring with faithfulness hooks, governance workflow with segregation of duties, provenance graphs. These are genuine differentiators. However, the "AI for architecture" space is emerging with competitors, and ArchLucid's differentiation narrative is stronger on paper than in demonstrable form. The finding engine template system (10 engines) is a differentiator but lacks industry-specific depth.

**Tradeoffs:** Deep architecture-specific workflow versus broader EA tool positioning. The narrow focus is a strength for pilot success but limits initial market addressability.

**Improvement Recommendations:**
- Create a live demo comparison that shows ArchLucid's replay/compare capabilities against a baseline Confluence workflow
- Strengthen the positioning around provenance and traceability — this is genuinely unique and maps to compliance buyer needs

---

### 6. Trustworthiness
| Attribute | Value |
|-----------|-------|
| **Score** | 48 |
| **Weight** | 3 |
| **Weighted Deficiency** | 156 |

**Justification:** All trust evidence is self-asserted. SOC 2 is a self-assessment, not a CPA attestation. The pen test (Aeronova Red Team LLC, 2026-Q2) is in flight but not complete. No named reference customer exists. The trust center is well-organized and transparent about its limitations ("self-asserted documentation, not a substitute for a CPA SOC 2 report"), which is honest but not confidence-inspiring for enterprise buyers. LLM outputs are explicitly not legal proof — this is the right disclosure but it limits the product's governance claims.

**Tradeoffs:** Transparency about trust gaps is the right approach, but it means the product cannot pass most enterprise procurement security reviews in its current state. The 25% trust discount in pricing honestly reflects this.

**Improvement Recommendations:**
- Complete the in-flight pen test and publish the redacted executive summary
- Advance the SOC 2 Type II timeline by selecting an auditor (owner-blocked)
- Add the pen test remediation tracker to demonstrate responsiveness to findings

---

### 7. Workflow Embeddedness
| Attribute | Value |
|-----------|-------|
| **Score** | 50 |
| **Weight** | 3 |
| **Weighted Deficiency** | 150 |

**Justification:** CloudEvents webhooks exist. Azure DevOps PR decoration is implemented. Microsoft Teams notifications are supported. The integration catalog lists many targets, but most are `[Planned]`. Jira, ServiceNow, and Confluence are all deferred to V1.1. For V1, customers must build their own bridges from webhooks to their ITSM tools. This is a workable posture for design-partner pilots but will be a deal-breaker for enterprises with established Jira/ServiceNow workflows who won't invest in custom integration work for an unproven tool.

**Tradeoffs:** Deferring ITSM integrations to V1.1 was a scope decision that keeps V1 focused, but it limits workflow embeddedness to Azure-native tools.

**Improvement Recommendations:**
- Publish a sample CloudEvents-to-Jira webhook consumer as a reference implementation for V1 customers
- Ensure Teams notification coverage is comprehensive (all finding types, governance events, digest)
- Document the V1.1 integration roadmap prominently so buyers know the timeline

---

### 8. Executive Value Visibility
| Attribute | Value |
|-----------|-------|
| **Score** | 65 |
| **Weight** | 4 |
| **Weighted Deficiency** | 140 |

**Justification:** The executive sponsor brief is well-written and avoids overclaiming. The sponsor PDF export is built. The demo preview endpoint bundles run + manifest + artifacts + timeline + explanation into a single payload. The "Email this run to your sponsor" banner with days-since-first-commit badge is thoughtful UX. However, the executive story relies on the buyer already recognizing the problem, and the worked example ROI is empty. No executive testimonial or case study exists.

**Tradeoffs:** The sponsor brief correctly avoids promising enterprise-wide transformation and focuses on bounded pilot value, which is honest but less exciting.

**Improvement Recommendations:**
- Populate the worked example ROI to give sponsors a concrete artifact
- Create a 2-slide executive summary that can be attached to the sponsor PDF

---

### 9. Usability
| Attribute | Value |
|-----------|-------|
| **Score** | 55 |
| **Weight** | 3 |
| **Weighted Deficiency** | 135 |

**Justification:** The operator shell uses progressive disclosure (Pilot links default, Operate links expand). The CLI provides scriptable access. Role-based shaping restricts advanced surfaces. The first-run wizard pre-selects a sample preset. However, the product surface is enormous (90+ API controllers, multiple CLI commands, complex architecture request schema). The operator atlas maps UI routes to API to CLI but represents a large learning curve. The "guided tour" is described but five tour steps are wrapped in `<TourStepPendingApproval>` markers pending owner copy approval.

**Tradeoffs:** Comprehensive feature set versus ease of first-use. Progressive disclosure helps but the underlying complexity is real.

**Improvement Recommendations:**
- Approve and activate the five guided tour steps to reduce first-use confusion
- Create a "3-click happy path" poster that shows the minimal steps from request to committed manifest
- Reduce the visible API surface in the trial experience

---

### 10. Security
| Attribute | Value |
|-----------|-------|
| **Score** | 62 |
| **Weight** | 3 |
| **Weighted Deficiency** | 114 |

**Justification:** Security architecture is thoughtfully designed: STRIDE threat model covering all trust boundaries, RLS with SESSION_CONTEXT for tenant isolation, fail-closed auth via AuthSafetyGuard, OWASP ZAP and Schemathesis in CI, LLM prompt redaction with deny-list patterns, private endpoints, managed identity for SQL/Blob, CORS deny-by-default, rate limiting, circuit breakers. However: RLS has known gaps per MULTI_TENANT_RLS.md §9 (some tables lack scope columns), DevelopmentBypass exists with guardrails, the pen test is in flight but incomplete, and SOC 2 is self-assessed only.

**Tradeoffs:** Defense-in-depth controls are present but not externally validated. The honest acknowledgment of residual risks (RLS gaps, uncovered tables) is mature engineering but leaves gaps.

**Improvement Recommendations:**
- Track and close the residual uncovered RLS tables documented in MULTI_TENANT_RLS.md §9
- Complete the pen test and establish a remediation tracking process
- Add CI-enforced RLS coverage assertions so new tables cannot be added without scope columns

---

### 11. Correctness
| Attribute | Value |
|-----------|-------|
| **Score** | 72 |
| **Weight** | 4 |
| **Weighted Deficiency** | 112 |

**Justification:** Strong correctness infrastructure: OpenAPI contract snapshot tests, Schemathesis property-based testing, golden corpus regression tests, agent output quality gates with semantic evaluation scoring, idempotency patterns (create run, webhook processing), optimistic concurrency on key tables. Agent output structural completeness is measured and gated. Explanation faithfulness is checked against findings text. However, code coverage sits at ~73% line (below the 79% strict gate), ArchLucid.Persistence is at ~40% line coverage, and 15 tests failed on the last local run.

**Tradeoffs:** The correctness infrastructure is excellent in design but the coverage gap means significant code paths are untested. The 79% gate is aspirational but not met.

**Improvement Recommendations:**
- Lift ArchLucid.Api per-package coverage to ≥63% (currently ~61%)
- Lift ArchLucid.Persistence per-package coverage to ≥63% (currently ~40%)
- Fix the 15 failing local tests (mostly SQL-integration tests without a reachable test catalog)

---

### 12. Compliance Readiness
| Attribute | Value |
|-----------|-------|
| **Score** | 50 |
| **Weight** | 2 |
| **Weighted Deficiency** | 100 |

**Justification:** SOC 2 self-assessment exists with compliance evidence matrix. CAIQ Lite and SIG Core pre-fills are prepared. DPA template exists. Subprocessors register is published. Trust center is public with evidence pack ZIP download. However, no SOC 2 Type II attestation exists. The pen test is in flight. The privacy notice is in draft (owner-blocked). GDPR/data residency considerations are acknowledged but not formally addressed beyond Azure region selection.

**Tradeoffs:** Pre-filling security questionnaires shows intent, but without a CPA-attested report, most enterprise procurement processes will stall.

**Improvement Recommendations:**
- Advance SOC 2 Type II auditor selection (owner-blocked)
- Complete pen test and publish executive summary under NDA
- Finalize privacy notice (owner-blocked for legal review)

---

### 13. Decision Velocity
| Attribute | Value |
|-----------|-------|
| **Score** | 55 |
| **Weight** | 2 |
| **Weighted Deficiency** | 90 |

**Justification:** Dry-run mode for governance submissions and promotions enables pre-validation. Preview endpoints show what-would-change before activation. The pilot success scorecard template structures the evaluation decision. However, the multi-stakeholder sales motion (EA, security, SRE, procurement) inherently slows decisions. No published customer timeline exists to set expectations.

**Tradeoffs:** The product's enterprise features (governance, audit, policy packs) are designed for large organizations where decisions are inherently slow. The pilot-first motion is the right response.

**Improvement Recommendations:**
- Publish a "typical pilot timeline" (30/60/90 day) with decision gates so buyers can plan
- Create a procurement fast-track package that bundles trust center + DPA + CAIQ in a single send

---

### 14. Interoperability
| Attribute | Value |
|-----------|-------|
| **Score** | 55 |
| **Weight** | 2 |
| **Weighted Deficiency** | 90 |

**Justification:** REST API with OpenAPI v3, versioned paths, API client NuGet package. AsyncAPI 2.6 for outbound webhooks. CloudEvents format for integration events. Azure DevOps PR decoration. Microsoft Teams notifications. CLI tool. Bruno collection for manual testing. However, no first-party Jira, ServiceNow, Confluence, or Slack integrations in V1. SCIM 2.0 is V1.1-committed. The API surface is comprehensive but deep — 90+ controllers is a large integration surface.

**Tradeoffs:** Comprehensive API versus pre-built integrations. The API-first approach enables any integration but shifts the burden to the customer.

**Improvement Recommendations:**
- Publish sample webhook consumers for Jira and ServiceNow as reference implementations
- Ensure the API client NuGet package covers the most common integration scenarios
- Document the top 5 integration patterns customers will need

---

### 15. Procurement Readiness
| Attribute | Value |
|-----------|-------|
| **Score** | 60 |
| **Weight** | 2 |
| **Weighted Deficiency** | 80 |

**Justification:** Procurement pack exists and is downloadable as a ZIP. Trust center is public. Order form template exists. DPA template exists. CAIQ Lite and SIG Core pre-fills are prepared. Evidence pack includes subprocessors register. However, the procurement pack leads with self-asserted evidence — no CPA attestation, no completed pen test report, no reference customer letter. The quote-request flow (`POST /v1/marketing/pricing/quote-request`) exists but CRM integration is pending (PENDING_QUESTIONS item 13).

**Tradeoffs:** Comprehensive procurement scaffolding versus missing third-party validation. The mechanics are ready; the evidence is not.

**Improvement Recommendations:**
- Add a "procurement FAQ" that proactively addresses the SOC 2 gap, pen test timeline, and reference availability
- Complete CRM/mailbox decisions for quote-request follow-up

---

### 16. Reliability
| Attribute | Value |
|-----------|-------|
| **Score** | 65 |
| **Weight** | 2 |
| **Weighted Deficiency** | 70 |

**Justification:** Circuit breakers for LLM calls. Polly retry policies with Simmy chaos injection for testing. Transactional outbox for event processing. Database failover documented with runbook. Health checks (live/ready) on API and worker. Degraded mode behavior documented. However, no production uptime data exists. The game-day log shows a placeholder entry (`2026-10-28-staging-placeholder.md`). Geo-failover drill is documented as a runbook but no completed drill evidence.

**Tradeoffs:** Reliability infrastructure is well-designed but unproven at production scale.

**Improvement Recommendations:**
- Execute the geo-failover drill and document results
- Run a game-day chaos scenario against staging
- Establish SLO targets backed by Prometheus alerting rules (infrastructure exists in `terraform-monitoring/prometheus_slo_rules.tf`)

---

### 17. Traceability
| Attribute | Value |
|-----------|-------|
| **Score** | 78 |
| **Weight** | 3 |
| **Weighted Deficiency** | 66 |

**Justification:** Strong traceability infrastructure: OTel trace IDs on every run (`Runs.OtelTraceId`), provenance graphs with algorithms for completeness analysis, decision traces on manifests, export records with replay semantics, comparison records with drift detection, correlation IDs on all API requests (`X-Correlation-ID`), build provenance metadata, agent execution traces with full prompt/response blob offload. The provenance completeness analyzer emits `archlucid_explainability_trace_completeness_ratio`.

**Tradeoffs:** Traceability is a genuine differentiator. The depth of the trace infrastructure exceeds most competitors.

**Improvement Recommendations:**
- Surface traceability metrics in the executive sponsor brief to strengthen the differentiability narrative
- Ensure provenance completeness thresholds are enforced (not just measured)

---

### 18. Data Consistency
| Attribute | Value |
|-----------|-------|
| **Score** | 70 |
| **Weight** | 2 |
| **Weighted Deficiency** | 60 |

**Justification:** Explicit data consistency matrix documenting strong/eventual/best-effort guarantees per aggregate. Optimistic concurrency via ROWVERSION on key tables. Transactional outbox for integration events. Orphan detection probes (`DataConsistencyOrphanProbeHostedService`) for ComparisonRecords, GoldenManifests, FindingsSnapshots. Hot-path read cache with documented staleness semantics and write-through invalidation. Read replica lag expectations documented (5s normal, 10–30s burst).

**Tradeoffs:** The consistency model is well-documented and honest about eventual paths. The orphan probe is detection-only — remediation is manual.

**Improvement Recommendations:**
- Add automated orphan remediation (quarantine or admin-authorized cleanup) beyond detection-only
- Add cache staleness monitoring metrics

---

### 19. Architectural Integrity
| Attribute | Value |
|-----------|-------|
| **Score** | 80 |
| **Weight** | 3 |
| **Weighted Deficiency** | 60 |

**Justification:** Clean 53-project decomposition with explicit boundaries: Core (domain primitives, diagnostics), Contracts (shared DTOs), Application (orchestration), Persistence (6 sub-projects by domain), Api (HTTP surface), AgentRuntime/AgentSimulator (agent execution), Decisioning (merge, validation, findings, governance), ContextIngestion, KnowledgeGraph, ArtifactSynthesis, Provenance, Retrieval. Architecture tests enforce dependency rules. 28+ ADRs document key decisions. C4 diagrams with ownership tables. Strangler fig pattern (ADR 0021/0028) for coordinator-to-authority migration completed successfully.

**Tradeoffs:** The decomposition is thorough but the number of projects (53) adds compilation and navigation overhead.

**Improvement Recommendations:**
- Consider whether some of the 6 Persistence sub-projects could be consolidated without losing boundary clarity
- Ensure the Architecture.Tests project covers the newest project additions

---

### 20. Commercial Packaging Readiness
| Attribute | Value |
|-----------|-------|
| **Score** | 72 |
| **Weight** | 2 |
| **Weighted Deficiency** | 56 |

**Justification:** Three-tier packaging (Team $436/mo example, Professional $2,331/mo example, Enterprise $60K–$250K/year). Machine-readable pricing JSON with CI guards for alignment. Feature gates defined per tier. Stripe integration code exists. Azure Marketplace listing planned with Partner Center plan names aligned. Pilot pricing ($15K guided, design partner 50% off). Expansion levers documented (seats, workspaces, tier upgrade, run overage). Sensitivity playbook for early deal signals.

**Tradeoffs:** Packaging is comprehensive on paper but unvalidated by real deals. The sensitivity playbook is theoretical.

**Improvement Recommendations:**
- Validate Team tier pricing against discretionary budget thresholds with at least 3 prospect conversations
- Complete the Stripe checkout integration for Team tier self-serve conversion

---

### 21. Explainability
| Attribute | Value |
|-----------|-------|
| **Score** | 72 |
| **Weight** | 2 |
| **Weighted Deficiency** | 56 |

**Justification:** Aggregate explanation endpoint with structured output (themes, overall assessment, risk posture, finding counts). Faithfulness checking against findings text via `ExplanationFaithfulnessChecker`. Deterministic fallback when LLM narrative has low faithfulness. Citation emission for UI chips. Provenance graphs explain the decision chain. Explanation schema is documented. However, LLM-generated narratives are inherently opaque, and the faithfulness heuristic is token-overlap based — not semantic verification.

**Tradeoffs:** The faithfulness fallback is a good safety net, but the gap between "token overlap" and "semantic accuracy" remains.

**Improvement Recommendations:**
- Consider adding a confidence interval to explanation outputs
- Document the faithfulness threshold and fallback behavior for operators

---

### 22. Azure Compatibility and SaaS Deployment Readiness
| Attribute | Value |
|-----------|-------|
| **Score** | 72 |
| **Weight** | 2 |
| **Weighted Deficiency** | 56 |

**Justification:** Container Apps for API and Worker hosting. Front Door with WAF at edge. 15 Terraform roots covering core, edge, container apps, SQL failover, monitoring, service bus, storage, key vault, OpenAI, Entra, logic apps, OTEL collector, private networking. Managed identity for SQL/Blob/Service Bus. Application Insights and Prometheus. CD pipeline documented. ACR image push. However, self-serve billing loop not fully in production. Custom domain attachment pending. `apply-saas.ps1` script exists but production deployment evidence is not confirmed.

**Tradeoffs:** Azure-native depth is excellent but the SaaS operational layer is not yet in steady-state production.

**Improvement Recommendations:**
- Complete custom domain attachment and TLS certificate provisioning
- Validate the full `apply-saas.ps1` deployment in staging with real DNS

---

### 23. Policy and Governance Alignment
| Attribute | Value |
|-----------|-------|
| **Score** | 72 |
| **Weight** | 2 |
| **Weighted Deficiency** | 56 |

**Justification:** Governance approval workflows with submit/approve/reject/activate lifecycle. Segregation of duties enforcement (self-approval blocked with audit event). Environment promotion gates (prod requires approved approval chain). Policy packs with versioning and assignment. Governance preview (manifest diff). Governance resolution reads. Dry-run mode for pre-validation. However, policy packs are currently operator-defined — no pre-built packs exist for common frameworks (TOGAF, C4, etc.).

**Tradeoffs:** The governance infrastructure is comprehensive, but its value depends on the operator defining meaningful policies. No starter packs lower the barrier.

**Improvement Recommendations:**
- Create 2–3 starter policy packs for common architecture governance frameworks
- Add policy pack templates to the `templates/` directory

---

### 24. Auditability
| Attribute | Value |
|-----------|-------|
| **Score** | 75 |
| **Weight** | 2 |
| **Weighted Deficiency** | 50 |

**Justification:** Append-only `dbo.AuditEvents` with DENY on delete/update roles. Audit coverage matrix tracking zero known gaps. Baseline mutation logging. Audit UI with CSV export. Keyset-cursor pagination on audit search. AuditEventTypes constants with CI count reconciliation guard. Governance self-approval blocking with durable audit event. Agent execution trace recording.

**Tradeoffs:** Audit infrastructure is strong. The audit-count CI guard is a smart integrity mechanism.

**Improvement Recommendations:**
- Add the `scripts/ci/assert_audit_const_count.py` guard to the CI pipeline if not already active
- Consider adding audit event tamper detection (hash chain) for compliance-sensitive deployments

---

### 25. Maintainability
| Attribute | Value |
|-----------|-------|
| **Score** | 75 |
| **Weight** | 2 |
| **Weighted Deficiency** | 50 |

**Justification:** 24+ Cursor rules enforcing code style (primary constructors, pattern matching, expression-bodied members, guard clauses, etc.). Modular design with clear project boundaries. TestSupport project for test utilities. Benchmarks project. Architecture tests. EditorConfig. Consistent naming conventions. Strong ADR discipline.

**Tradeoffs:** The code style rules are thorough and enforced, which improves consistency but adds onboarding cost for new contributors.

**Improvement Recommendations:**
- Continue maintaining architecture test coverage as new projects are added
- Document the most common contributor workflows in the contributor index

---

### 26. AI/Agent Readiness
| Attribute | Value |
|-----------|-------|
| **Score** | 75 |
| **Weight** | 2 |
| **Weighted Deficiency** | 50 |

**Justification:** Agent simulator for deterministic testing. Agent output quality gates with structural completeness ratio and semantic evaluation scoring. Golden cohort regression tests. LLM cost estimation per call. Token quota tracking with tenant-level daily budget. Agent execution trace recording with blob offload. Multiple agent types (topology, cost, compliance, critic). Prompt redaction with deny-list patterns. Content safety guard integration point. Circuit breakers for LLM calls.

**Tradeoffs:** The agent infrastructure is mature for a pre-production product. The simulator-first testing approach is sound.

**Improvement Recommendations:**
- Add more finding engine types beyond the current 10
- Strengthen the golden cohort with real Azure OpenAI baseline regression (nightly gate exists but depends on `--strict-real` opt-in)

---

### 27. Cognitive Load
| Attribute | Value |
|-----------|-------|
| **Score** | 45 |
| **Weight** | 1 |
| **Weighted Deficiency** | 55 |

**Justification:** The product imposes high cognitive load: 90+ API controllers, 53 projects, 455+ docs, 28+ ADRs, multiple CLI commands, complex architecture request schema, multiple execution modes (simulator vs real), governance lifecycle concepts, manifest versioning, and a pricing model with workspaces + seats + runs. Progressive disclosure in the UI helps, but the underlying conceptual model is large. The READ_THIS_FIRST.md routing is smart but routes to many deep documents.

**Tradeoffs:** Comprehensive feature set versus first-user experience. The product tries to serve multiple personas (operator, architect, sponsor, security reviewer, contributor) which multiplies the concept space.

**Improvement Recommendations:**
- Create a single-page "ArchLucid in 60 seconds" that covers the three core concepts (request → run → manifest)
- Reduce the visible concept surface for trial users to Pilot layer only

---

### 28. Template and Accelerator Richness
| Attribute | Value |
|-----------|-------|
| **Score** | 45 |
| **Weight** | 1 |
| **Weighted Deficiency** | 55 |

**Justification:** 10 finding engines exist. One finding engine template in `templates/archlucid-finding-engine/`. Sample presets for the first-run wizard. Demo seed with Contoso retail fictional data. But no industry-specific architecture request templates (financial services, healthcare, retail). No pre-built governance policy packs. No starter architecture request patterns. The blank-page problem is real for evaluators.

**Tradeoffs:** Generic flexibility versus vertical acceleration. The product needs templates to reduce time-to-value but vertical templates require domain expertise.

**Improvement Recommendations:**
- Create 3–5 architecture request templates for common scenarios (greenfield web app, cloud migration, microservices decomposition)
- Add starter governance policy packs

---

### 29. Accessibility
| Attribute | Value |
|-----------|-------|
| **Score** | 45 |
| **Weight** | 1 |
| **Weighted Deficiency** | 55 |

**Justification:** The architecture documentation mentions Axe tests and Playwright operator journeys for the operator shell (Next.js UI). Vitest is listed for UI unit tests. However, I cannot locate completed WCAG 2.1 AA compliance test results or an accessibility statement. The UI's progressive disclosure model and role-based shaping could introduce accessibility issues if not tested with screen readers.

**Tradeoffs:** Accessibility testing is mentioned but evidence of comprehensive coverage is thin.

**Improvement Recommendations:**
- Run a comprehensive Axe audit against the operator shell and publish results
- Add WCAG 2.1 AA compliance as a CI gate for UI changes
- Create an accessibility statement for the trust center

---

### 30. Availability
| Attribute | Value |
|-----------|-------|
| **Score** | 60 |
| **Weight** | 1 |
| **Weighted Deficiency** | 40 |

**Justification:** Geo-failover drill runbook exists. Read replicas documented. Health checks (live/ready) on API. Container Apps multi-revision deployment. SQL failover group infrastructure in Terraform. RTO/RPO targets documented by tier. However, no production uptime data. Game-day log is a placeholder. SLA summary exists but is based on Azure-composed SLAs, not measured availability.

**Tradeoffs:** Infrastructure for HA is in place; operational proof is not.

**Improvement Recommendations:**
- Execute the geo-failover drill in staging and document results
- Set up synthetic monitoring (uptime checks) for staging

---

### 31. Self-Sufficiency
| Attribute | Value |
|-----------|-------|
| **Score** | 60 |
| **Weight** | 1 |
| **Weighted Deficiency** | 40 |

**Justification:** CLI `archlucid doctor` for self-diagnosis. `archlucid support-bundle --zip` for support package creation. Health endpoints. In-app diagnostics controllers. Correlation IDs for request tracing. Extensive operator runbooks. However, the documentation volume (455+ files) can itself become an obstacle to self-sufficiency — operators may not find the right doc.

**Tradeoffs:** Rich self-service tooling versus documentation discoverability.

**Improvement Recommendations:**
- Add a searchable help index or FAQ to the operator shell
- Ensure `archlucid doctor` covers the most common first-run failures

---

### 32. Cost-Effectiveness
| Attribute | Value |
|-----------|-------|
| **Score** | 60 |
| **Weight** | 1 |
| **Weighted Deficiency** | 40 |

**Justification:** LLM cost estimation per call (`LlmCostEstimator`). Token quota tracking with per-tenant daily budget. Consumption budgets in Terraform (`consumption_budget.tf`). Cost preview endpoint for operators (`AgentExecutionCostPreviewController`). Pricing sensitivity playbook. However, no production cost data exists. LLM costs scale with usage and model choice. Azure infrastructure costs (SQL, Container Apps, Front Door, OpenAI) are not trivially predictable for operators.

**Tradeoffs:** Cost controls exist but are unvalidated at scale.

**Improvement Recommendations:**
- Publish a reference cost model for a typical deployment (per-month Azure spend estimate)
- Add cost dashboards to the monitoring Terraform module

---

### 33. Scalability
| Attribute | Value |
|-----------|-------|
| **Score** | 62 |
| **Weight** | 1 |
| **Weighted Deficiency** | 38 |

**Justification:** Container Apps horizontal scaling. Read replicas for read-heavy workloads. Service Bus for async processing. Hot-path cache for frequent reads. SQL index inventory documented. Run archival for data lifecycle management. However, no load testing evidence. The 2,000 run/month fair-use cap for Enterprise suggests scale limits. No documented throughput targets.

**Tradeoffs:** Scale infrastructure exists but is untested at target volumes.

**Improvement Recommendations:**
- Run load tests against the run-create → execute → commit pipeline
- Document throughput targets (runs/hour, concurrent users) for each tier

---

### 34–46. Remaining qualities (lower weighted deficiency)

| Quality | Score | Weight | Weighted Deficiency |
|---------|-------|--------|-------------------|
| Stickiness | 65 | 1 | 35 |
| Performance | 65 | 1 | 35 |
| Change Impact Clarity | 68 | 1 | 32 |
| Manageability | 68 | 1 | 32 |
| Supportability | 70 | 1 | 30 |
| Extensibility | 70 | 1 | 30 |
| Deployability | 72 | 1 | 28 |
| Evolvability | 72 | 1 | 28 |
| Testability | 73 | 1 | 27 |
| Observability | 78 | 1 | 22 |
| Azure Ecosystem Fit | 78 | 1 | 22 |
| Modularity | 80 | 1 | 20 |
| Documentation | 82 | 1 | 18 |

These qualities are the product's engineering strengths. Documentation (82), Modularity (80), Observability (78), and Azure Ecosystem Fit (78) represent genuine competitive advantages.

---

## 3. Weighted Readiness Calculation

| Category | Weighted Score | Max Possible | Contribution |
|----------|---------------|--------------|--------------|
| Commercial (weight 40) | 2,334 | 4,000 | 58.35% |
| Enterprise (weight 25) | 1,490 | 2,500 | 59.60% |
| Engineering (weight 37) | 2,907 | 3,700 | 78.57% |
| **Total** | **6,731** | **10,200** | **65.99%** |

---

## 4. Top 10 Most Important Weaknesses

1. **No market validation whatsoever.** Zero customers, zero revenue, zero reference deployments. All go-to-market planning is theoretical. This is the single largest risk across all dimensions.

2. **Trust evidence is entirely self-asserted.** SOC 2 is a self-assessment, pen test is in flight, no third-party has validated any security claim. Enterprise procurement will stall here.

3. **The product category does not exist in buyer vocabulary.** "AI-assisted architecture workflow" requires creating a category, not just selling a product. Buyers don't budget for this.

4. **ITSM integration gap in V1.** Jira, ServiceNow, and Confluence are deferred to V1.1. Enterprises with these tools will not invest in webhook bridges for an unproven product.

5. **Code coverage below strict gates.** 73% line coverage vs 79% target, Persistence at 40%. The testing infrastructure is strong but the coverage gaps are in persistence and API — the most critical production paths.

6. **Self-serve trial-to-paid loop not fully operational.** The trial is designed but the billing conversion path (Stripe checkout for Team tier) is not demonstrated end-to-end in production.

7. **ROI claims are unsubstantiated.** The ROI model math is reasonable but the worked example has placeholder data and no customer has validated the time savings.

8. **Cognitive load is prohibitively high for evaluators.** 90+ controllers, 53 projects, 455 docs, multiple personas — first-time users face a wall of complexity before they understand the product.

9. **No production operational data exists.** Reliability, performance, availability, and scalability claims are based on infrastructure design, not measured behavior. Game-day log is a placeholder.

10. **Privacy notice is in draft, blocked on legal review.** This is a compliance gap that will surface in every enterprise deal and trial signup.

---

## 5. Top 5 Monetization Blockers

1. **No reference customer to trigger the 15% reference-discount gate or provide social proof.** Every deal requires selling the category from scratch without a peer example.

2. **Self-serve billing loop (trial → Stripe checkout → paid tenant) not in production.** The lowest-friction revenue path is blocked, forcing all deals through high-touch sales.

3. **SOC 2 Type II not attested.** The 25% trust discount in pricing exists because of this. Clearing this gate would defensibly raise list prices 25%.

4. **ROI worked example contains placeholder data.** Sponsors cannot see concrete (even fictional) numbers to anchor budget conversations.

5. **CRM and quote-request follow-up infrastructure not configured.** `POST /v1/marketing/pricing/quote-request` exists but the owner mailbox and CRM integration are pending (PENDING_QUESTIONS item 13). Inbound interest cannot be systematically followed up.

---

## 6. Top 5 Enterprise Adoption Blockers

1. **No third-party security attestation.** Enterprise security reviewers will require SOC 2 Type II or equivalent. Self-assessment and in-flight pen test are not sufficient for procurement approval.

2. **No ITSM integration in V1.** Enterprises with Jira, ServiceNow, or Confluence workflows will not adopt a tool that requires custom webhook bridges. V1.1 timing is unspecified.

3. **No existing customer reference.** Enterprise buyers require peer validation. No logo, no case study, no reference call — the "first mover" objection is strong.

4. **Privacy notice in draft.** Privacy and data processing terms must be finalized before enterprises can complete procurement. The DPA template exists but the privacy notice is pending legal approval.

5. **Entra ID dependency limits non-Microsoft shops.** Authentication requires Entra ID (Microsoft account or email/password). Organizations on Okta, Auth0, or Google Workspace face a friction point. Generic OIDC is on the roadmap but not in V1.

---

## 7. Top 5 Engineering Risks

1. **Persistence layer coverage at 40% masks potential data integrity bugs.** The most critical production path (SQL writes via Dapper) has the lowest coverage. Data corruption or loss bugs would be catastrophic for a product built on audit and traceability.

2. **LLM prompt injection mitigations are deny-list based.** The `PromptRedactor` uses pattern matching against a deny list. Sophisticated prompt injection attacks may bypass pattern-based defenses. No formal adversarial testing of the LLM pipeline is documented.

3. **RLS has known uncovered tables.** MULTI_TENANT_RLS.md §9 tracks tables without scope columns. Cross-tenant data leakage through these tables is a latent risk.

4. **No production-scale testing.** Circuit breakers, retry policies, and chaos injection exist but have not been validated under realistic load. Container Apps scaling behavior under sustained throughput is untested.

5. **DevelopmentBypass auth mode exists.** While documented as non-production-only with guardrails, the presence of an auth bypass mode in the codebase is a risk if misconfigured in deployment. The AuthSafetyGuard should fail loudly in production environments.

---

## 8. Most Important Truth

**ArchLucid is a superbly engineered product with no evidence that anyone will pay for it.** The engineering quality, documentation depth, and architectural rigor exceed what most funded startups achieve — but every commercial and trust metric is at zero. The product needs one customer more than it needs one more feature. Until the first design-partner deal closes, the ROI model is theory, the pricing is speculation, and the trust center is a library of self-assessments. The immediate priority should be ruthlessly narrowing focus to whatever it takes to close the first deal and generate the first reference customer evidence.

---

## Deferred Scope Uncertainty

I located the deferred items register at `docs/library/V1_DEFERRED.md` and confirmed the following are explicitly deferred:

- **V1.1:** Jira connector, ServiceNow connector, Confluence connector, SCIM 2.0 Service Provider
- **V2:** Slack connector
- **V1.1/TBD:** SOC 2 Type II attestation, formal pen test program (beyond the in-flight 2026-Q2 engagement)
- **Deferred:** Privacy notice finalization (owner-blocked), Entra app gallery listing (owner-undecided)

These items were **not** penalized in scoring. I was able to locate the markdown for all referenced deferred items.

---

## 9. Top Improvement Opportunities

### Improvement 1: Populate Worked Example ROI From Demo Seed

**Why it matters:** The single biggest proof-of-ROI gap is that `docs/go-to-market/WORKED_EXAMPLE_ROI.md` contains placeholder text (`(pending) | Run scripts/ops/generate-worked-example-roi.ps1`). Every sponsor conversation requires concrete numbers, even fictional ones.

**Expected impact:** Directly improves Proof-of-ROI Readiness (+8–12 pts), Executive Value Visibility (+5–8 pts), Marketability (+3–5 pts). Weighted readiness impact: +0.5–0.8%.

**Affected qualities:** Proof-of-ROI Readiness, Executive Value Visibility, Marketability, Time-to-Value

**Status:** Fully actionable now.

**Cursor Prompt:**
```
Run the worked example ROI generation pipeline against the demo seed data. Steps:

1. Read `scripts/ops/generate-worked-example-roi.ps1` to understand the pipeline.
2. If the script requires a running Docker compose instance, document the exact invocation sequence.
3. If the script can run against already-seeded data, execute it.
4. After generation, read the output and update `docs/go-to-market/WORKED_EXAMPLE_ROI.md` to replace the `(pending)` placeholder rows in the `<!-- BEGIN_AUTOGENERATED_ROI_MD -->` block with the generated values.
5. If a PDF was generated, confirm it is placed at `docs/go-to-market/WORKED_EXAMPLE_ROI.pdf`.

Acceptance criteria:
- The markdown table between the autogenerated markers contains real metric rows (not `(pending)`)
- Values are clearly labeled as Contoso sample data (not real customer outcomes)
- The existing honesty disclaimer at the top of the file is preserved
- No changes to the generation scripts themselves

Constraints:
- Do NOT modify `scripts/ops/generate-worked-example-roi.ps1`
- Do NOT modify `scripts/ops/value_report_docx_extract_to_md.py`
- Do NOT change the ROI model numbers in `docs/go-to-market/ROI_MODEL.md`
- Preserve the `<!-- BEGIN_AUTOGENERATED_ROI_MD -->` / `<!-- END_AUTOGENERATED_ROI_MD -->` fences
```

---

### Improvement 2: Lift ArchLucid.Persistence Code Coverage to Per-Package Gate (≥63%)

**Why it matters:** ArchLucid.Persistence is at ~40% line coverage — the lowest of any product assembly and well below the 63% per-package gate. This is the SQL/Dapper data access layer; uncovered paths here risk data integrity bugs in the most critical production code.

**Expected impact:** Directly improves Correctness (+5–8 pts), Reliability (+3–5 pts), Testability (+3–5 pts). Weighted readiness impact: +0.4–0.7%.

**Affected qualities:** Correctness, Reliability, Testability, Data Consistency

**Status:** Fully actionable now.

**Cursor Prompt:**
```
Increase the line coverage of the `ArchLucid.Persistence` assembly from ~40% to ≥63% by adding targeted unit tests for the most impactful uncovered code paths.

Steps:
1. Run `dotnet test ArchLucid.Persistence.Tests -c Release --settings coverage.runsettings --collect:"XPlat Code Coverage"` and identify the classes/methods with zero or near-zero coverage.
2. Prioritize test creation for:
   a. SQL repository methods that perform INSERT/UPDATE/DELETE (data mutation paths)
   b. Repository methods used in the authority pipeline (run create, commit, manifest save)
   c. Any repository method called from the governance workflow
   d. Data archival cascade logic
   e. Tenant hard-purge service SQL paths
3. Create unit tests using the existing InMemory provider implementations where they exist (check `ArchLucid.Persistence` for `InMemory*` classes).
4. For SQL-specific paths that require a database, create tests that mock the `IDbConnection` / use the existing test infrastructure patterns in `ArchLucid.Persistence.Tests`.
5. After adding tests, re-measure coverage and verify the per-package line is ≥63%.

Acceptance criteria:
- `ArchLucid.Persistence` per-package line coverage ≥ 63% in merged Cobertura
- All new tests pass in both Debug and Release configurations
- No existing tests broken
- Tests follow existing patterns in `ArchLucid.Persistence.Tests`
- do not use ConfigureAwait(false) in tests

Constraints:
- Do NOT modify production code to increase coverage unless fixing an actual bug
- Do NOT add `[ExcludeFromCodeCoverage]` to production classes to game the metric
- Do NOT change the coverage gate thresholds
- Follow existing test naming conventions and project structure
```

---

### DEFERRED Improvement 3: Complete SOC 2 Type II Attestation

**Title:** DEFERRED: Complete SOC 2 Type II Attestation

**Reason deferred:** Requires selection of a CPA auditor firm, contract negotiation, and engagement kickoff — none of which can proceed without owner involvement and budget approval.

**Information needed:** (a) Preferred auditor shortlist or constraints, (b) Budget ceiling for the engagement, (c) Target timeline for Type I vs Type II, (d) Whether to pursue a readiness assessment first.

---

### DEFERRED Improvement 4: Secure First Reference Customer

**Title:** DEFERRED: Secure First Reference Customer

**Reason deferred:** Requires sales/BD outreach, prospect identification, and commercial negotiation — entirely owner-driven activities that cannot be executed by engineering.

**Information needed:** (a) Target prospect list or ICP criteria for the design-partner program, (b) Whether the $15K guided pilot pricing is approved for outbound offers, (c) Whether the 50% design-partner discount (3-slot cap) is approved to offer.

---

### Improvement 5: Create Architecture Request Templates for Common Scenarios

**Why it matters:** Evaluators face a blank-page problem when creating their first architecture request. The finding engine template directory has only one entry. Pre-built request templates for common scenarios (greenfield web app, cloud migration, microservices) would dramatically reduce time-to-first-committed-manifest.

**Expected impact:** Directly improves Template and Accelerator Richness (+15–20 pts), Time-to-Value (+5–8 pts), Adoption Friction (+3–5 pts). Weighted readiness impact: +0.3–0.5%.

**Affected qualities:** Template and Accelerator Richness, Time-to-Value, Adoption Friction, Usability

**Status:** Fully actionable now.

**Cursor Prompt:**
```
Create 3 architecture request template JSON files that operators can use as starting points in the first-run wizard or CLI. Place them in a new directory `templates/architecture-requests/`.

Templates to create:
1. `greenfield-web-app.json` — A three-tier web application on Azure (App Service + SQL + CDN) with typical compliance requirements.
2. `cloud-migration-lift-shift.json` — An on-premises .NET application being migrated to Azure Container Apps with managed database.
3. `microservices-decomposition.json` — Breaking a monolith into 4 bounded-context microservices with Service Bus messaging.

For each template:
1. Read `ArchLucid.Contracts` to understand the `ArchitectureRequest` schema (look for the DTO or record definition).
2. Populate realistic fields based on the schema: name, description, context documents, constraints, compliance requirements, cloud provider hints.
3. Add a `_template` metadata section with: `title`, `description`, `difficulty` (beginner/intermediate), `estimatedRunTimeMinutes`, `prerequisites`.
4. Include inline comments or a companion `README.md` explaining how to customize each template.

Acceptance criteria:
- 3 valid JSON files that deserialize correctly against the `ArchitectureRequest` contract
- Each file includes realistic, non-trivial content (not just field names with empty strings)
- A `templates/architecture-requests/README.md` lists all templates with one-line descriptions
- Templates reference Contoso-style fictional company names (not real companies)

Constraints:
- Do NOT modify the `ArchitectureRequest` contract or schema
- Do NOT modify the first-run wizard code (that integration is a follow-on)
- Do NOT add NuGet dependencies
- Keep each template under 200 lines of JSON
```

---

### Improvement 6: Add Comprehensive Accessibility Audit to Operator Shell CI

**Why it matters:** Accessibility scored 45 — the joint-lowest score in the assessment. Enterprise buyers increasingly require WCAG 2.1 AA compliance. Public sector buyers mandate it. An Axe-based CI gate would catch regressions and demonstrate compliance intent.

**Expected impact:** Directly improves Accessibility (+15–20 pts), Compliance Readiness (+3–5 pts), Usability (+2–3 pts). Weighted readiness impact: +0.2–0.4%.

**Affected qualities:** Accessibility, Compliance Readiness, Usability, Procurement Readiness

**Status:** Fully actionable now.

**Cursor Prompt:**
```
Add an automated WCAG 2.1 AA accessibility audit to the operator shell (archlucid-ui) CI pipeline using @axe-core/playwright.

Steps:
1. Check if `@axe-core/playwright` is already a dependency in `archlucid-ui/package.json`. If not, add it as a dev dependency.
2. Create a new test file `archlucid-ui/e2e/accessibility.spec.ts` that:
   a. Navigates to key pages: `/`, `/runs`, `/runs/new` (first-run wizard), `/settings`, `/audit`
   b. Runs `checkA11y()` (or equivalent @axe-core/playwright API) on each page
   c. Asserts zero critical or serious violations at WCAG 2.1 AA level
   d. Allows `minor` and `moderate` violations as warnings (logged but not failing)
3. If a Playwright config exists (`archlucid-ui/playwright.config.ts`), add the accessibility tests to the existing project configuration.
4. Create `docs/library/ACCESSIBILITY_AUDIT.md` documenting:
   a. Which pages are audited
   b. What WCAG level is enforced
   c. How to run the audit locally
   d. How to add new pages to the audit

Acceptance criteria:
- Accessibility tests run as part of the existing Playwright test suite
- Tests cover at least 5 key operator shell pages
- Zero critical/serious WCAG 2.1 AA violations pass
- A documentation file explains the audit process
- Tests can run headlessly in CI

Constraints:
- Do NOT modify existing component implementations to fix violations in this PR (that is follow-on work)
- Do NOT add accessibility tests for marketing pages (only operator shell)
- Do NOT change the Playwright configuration for existing tests
- If @axe-core/playwright is not available, use axe-playwright instead
```

---

### Improvement 7: Create Single-Page Contributor Quick-Start Index

**Why it matters:** Cognitive load (45) is a major weakness. New contributors and evaluators face 455+ docs. A single-page index (≤100 lines, CI-enforced per PENDING_QUESTIONS Improvement 7) that maps the 5 most common tasks to their entry documents would reduce time-to-productive.

**Expected impact:** Directly improves Cognitive Load (+5–8 pts), Usability (+3–5 pts), Documentation (+2–3 pts). Weighted readiness impact: +0.1–0.3%.

**Affected qualities:** Cognitive Load, Usability, Documentation, Self-Sufficiency

**Status:** Fully actionable now.

**Cursor Prompt:**
```
Create `docs/CONTRIBUTOR_ON_ONE_PAGE.md` — a single-page contributor quick-start index that fits within the 100-line CI guard cap decided in PENDING_QUESTIONS Improvement 7a.

Content requirements (all within ≤100 lines of markdown):
1. One-sentence product description
2. Five-task table: "I want to..." → "Start here" with links:
   a. "Run the product locally" → `docs/engineering/FIRST_30_MINUTES.md`
   b. "Understand the architecture" → `docs/ARCHITECTURE_ON_ONE_PAGE.md`
   c. "Write and run tests" → `docs/library/TEST_STRUCTURE.md`
   d. "Deploy to Azure" → `docs/engineering/DEPLOYMENT.md`
   e. "Understand the API" → `docs/library/API_CONTRACTS.md`
3. Build command (one-liner): `dotnet build ArchLucid.sln`
4. Test command (one-liner): `dotnet test ArchLucid.sln`
5. Docker compose (one-liner): `docker compose up -d`
6. Link to `docs/READ_THIS_FIRST.md` for deeper navigation
7. Link to `docs/library/GLOSSARY.md` for terminology

Also create `scripts/ci/assert_contributor_on_one_page_size.py` that:
1. Reads `docs/CONTRIBUTOR_ON_ONE_PAGE.md`
2. Counts non-empty lines
3. Exits with code 1 if count > 100
4. Prints the line count to stdout

Acceptance criteria:
- `docs/CONTRIBUTOR_ON_ONE_PAGE.md` exists and is ≤100 lines
- CI script validates the line count
- All linked documents exist in the repository
- No broken relative links

Constraints:
- Do NOT exceed 100 lines (including blank lines and headers)
- Do NOT duplicate content from linked documents
- Do NOT include code examples beyond the three one-liners
- Use relative links, not absolute GitHub URLs
```

---

### Improvement 8: Add Pen Test Remediation Tracking Scaffold

**Why it matters:** The pen test (Aeronova Red Team LLC, 2026-Q2) is in flight. When findings arrive, having a structured remediation tracker ready will accelerate the path from finding to fix. This directly addresses the trust gap.

**Expected impact:** Directly improves Security (+3–5 pts), Trustworthiness (+3–5 pts), Compliance Readiness (+2–3 pts). Weighted readiness impact: +0.2–0.4%.

**Affected qualities:** Security, Trustworthiness, Compliance Readiness, Procurement Readiness

**Status:** Fully actionable now.

**Cursor Prompt:**
```
Create the pen test remediation tracking infrastructure so findings from the in-flight 2026-Q2 pen test (Aeronova Red Team LLC) can be systematically tracked.

Steps:
1. Create `docs/security/pen-test-summaries/REMEDIATION_TRACKER.md` with:
   a. A table template: `| Finding ID | Severity | Title | Status | Owner | Target Date | Verification | Notes |`
   b. Status values: `Open`, `In Progress`, `Remediated`, `Accepted Risk`, `Deferred`
   c. A section explaining the remediation workflow (find → triage → assign → fix → verify → close)
   d. A link to `docs/security/pen-test-summaries/2026-Q2-SOW.md` for engagement context
   e. A note that redacted findings stay under NDA and are not published in the public trust center

2. Create `scripts/ci/assert_pen_test_remediation_no_stale.py` that:
   a. Parses the remediation tracker markdown table
   b. Warns (non-blocking) if any `Open` finding has a `Target Date` in the past
   c. Fails (blocking) if any `Critical` severity finding has status `Open` for more than 30 days

3. Update `docs/trust-center.md` to add a row in the posture summary table:
   - Control: "Penetration test remediation tracking"
   - Status: "Active"
   - Evidence: link to REMEDIATION_TRACKER.md
   - Last reviewed: today's date

Acceptance criteria:
- Remediation tracker template exists with the correct table structure
- CI script parses the table correctly (test with 2–3 sample rows)
- Trust center references the new tracker
- No existing files are broken

Constraints:
- Do NOT add any actual pen test findings (the engagement is in flight)
- Do NOT modify the 2026-Q2-SOW.md
- Do NOT change the trust center's honesty posture (self-asserted labels stay accurate)
- Keep the CI script advisory (warning) for now — blocking enforcement comes after findings arrive
```

---

### Improvement 9: Add CloudEvents-to-Jira Webhook Bridge Reference Implementation

**Why it matters:** Workflow Embeddedness scored 50. Jira is deferred to V1.1, but V1 customers need a path. A reference implementation showing how to consume ArchLucid CloudEvents webhooks and create Jira issues would reduce adoption friction for the most common ITSM integration.

**Expected impact:** Directly improves Workflow Embeddedness (+5–8 pts), Interoperability (+3–5 pts), Adoption Friction (+2–3 pts). Weighted readiness impact: +0.2–0.4%.

**Affected qualities:** Workflow Embeddedness, Interoperability, Adoption Friction, Customer Self-Sufficiency

**Status:** Fully actionable now.

**Cursor Prompt:**
```
Create a reference implementation showing how to bridge ArchLucid CloudEvents webhooks to Jira issue creation. Place it in `samples/webhook-to-jira/`.

Steps:
1. Read `docs/contracts/archlucid-asyncapi-2.6.yaml` to understand the outbound webhook payload schema.
2. Read `docs/library/INTEGRATION_EVENTS_AND_WEBHOOKS.md` for webhook signing (HMAC) and delivery semantics.
3. Create a minimal Node.js or Python application (choose based on broadest appeal) that:
   a. Receives HTTP POST from ArchLucid webhook
   b. Validates the HMAC signature header
   c. Parses the CloudEvents envelope
   d. Maps finding severity to Jira priority
   e. Creates a Jira issue via REST API (with placeholder Jira URL and credentials from env vars)
   f. Returns 200 to acknowledge receipt
4. Include a `README.md` with:
   a. What this sample does
   b. Prerequisites (Jira Cloud instance, API token)
   c. Environment variables to set
   d. How to run locally
   e. How to deploy to Azure Functions or Azure Container Apps
   f. Caveat that this is a V1 bridge — native Jira connector ships in V1.1
5. Include a `docker-compose.yml` for local testing with a mock Jira endpoint (optional).

Acceptance criteria:
- Working sample application that compiles/runs
- HMAC validation against the ArchLucid webhook signature
- CloudEvents payload correctly parsed
- Jira issue creation API call correctly formed (can use a mock for testing)
- README is clear and complete
- Explicitly labeled as a sample/reference, not a supported product feature

Constraints:
- Do NOT add this as a project reference in `ArchLucid.sln`
- Do NOT add dependencies to the main ArchLucid projects
- Keep the sample under 300 lines of code (excluding README)
- Do NOT commit real Jira credentials
```

---

### Improvement 10: Automate Data Consistency Orphan Remediation

**Why it matters:** The orphan detection probes (`DataConsistencyOrphanProbeHostedService`) currently detect orphaned rows but do not remediate them. Admin endpoints for remediation exist (`AdminDataConsistencyOrphanRemediationEndpointsIntegrationTests`) but the automated workflow is incomplete. Data consistency scored 70 — closing the detection-to-remediation loop improves reliability and operator confidence.

**Expected impact:** Directly improves Data Consistency (+5–8 pts), Reliability (+3–5 pts), Manageability (+2–3 pts). Weighted readiness impact: +0.2–0.4%.

**Affected qualities:** Data Consistency, Reliability, Manageability, Supportability

**Status:** Fully actionable now.

**Cursor Prompt:**
```
Enhance the data consistency orphan detection system to support automated quarantine remediation when configured.

Steps:
1. Read `ArchLucid.Core/Diagnostics/ArchLucidInstrumentation.cs` for the existing `archlucid_data_consistency_orphans_detected_total` and `archlucid_data_consistency_alerts_total` counters.
2. Read the `DataConsistencyOrphanProbeHostedService` (search for it in the Persistence or Host.Composition projects) to understand the current detection logic.
3. Read `AdminDataConsistencyOrphanRemediationEndpointsIntegrationTests` to understand the existing admin remediation surface.
4. Add a `DataConsistency:Enforcement:AutoQuarantine` boolean configuration option (default `false`) that, when enabled:
   a. Moves detected orphan rows to a `dbo.QuarantinedOrphans` staging table (or marks them with a `QuarantinedUtc` column) instead of just counting them
   b. Emits an `archlucid_data_consistency_orphans_quarantined_total` counter per table/column
   c. Logs a structured warning with the quarantined row IDs
   d. Does NOT delete the rows — quarantine is a soft marker for admin review
5. Add a test in `ArchLucid.Persistence.Tests` or the appropriate test project verifying the quarantine behavior.
6. Update `docs/library/DATA_CONSISTENCY_MATRIX.md` to document the new quarantine mode.

Acceptance criteria:
- New configuration option `DataConsistency:Enforcement:AutoQuarantine` controls the behavior
- Default is `false` (preserves current detection-only behavior)
- When enabled, orphans are quarantined (not deleted)
- New Prometheus counter tracks quarantined rows
- At least one test verifies the quarantine path
- Documentation updated

Constraints:
- Do NOT enable auto-quarantine by default
- Do NOT delete any data — quarantine only
- Do NOT change the existing detection-only behavior when the feature is off
- Do NOT modify existing tests
- Follow existing code patterns for hosted services and configuration
```

---

## 10. Pending Questions for Later

### Improvement 3 (SOC 2 Type II Attestation)
- Do you have a preferred auditor shortlist or constraints (Big 4, regional, SaaS-specialized)?
- What is the budget ceiling for the SOC 2 engagement?
- Do you want a readiness assessment before Type I, or go directly to Type II?
- Is there a target date for having the attestation available to prospects?

### Improvement 4 (First Reference Customer)
- Do you have a target prospect list or ICP criteria for the design-partner program?
- Is the $15K guided pilot pricing approved for outbound offers today?
- Are all 3 design-partner slots (50% off Professional, 12 months) still available?
- Do you have a preferred industry vertical for the first reference?

### General (Improvement 5 — Tour Copy)
- Are you ready to review and approve the five guided-tour step copies so the `<TourStepPendingApproval>` wrappers can be removed? (Per PENDING_QUESTIONS Improvement 5, all five should land in one PR after approval.)

### General (Privacy Notice)
- Do you have a legal reviewer or external counsel identified for the privacy notice finalization?
- Is there a timeline for privacy notice completion?

### General (CRM Integration — PENDING_QUESTIONS item 13)
- What CRM system will receive quote requests from `POST /v1/marketing/pricing/quote-request`?
- What mailbox/distribution list should receive quote-request notifications?
