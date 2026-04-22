/**
 * Head-to-head rows for the public /why page. Competitor cells summarize
 * docs/go-to-market/COMPETITIVE_LANDSCAPE.md \u00a72.1. ArchLucid cells summarize
 * docs/go-to-market/COMPETITIVE_LANDSCAPE.md \u00a73 with repo-path citations only.
 * Every row includes a procurement `evidenceAnchor`: a live `GET/POST /v1/...` path
 * plus a static asset under `public/marketing/why/` (replace SVG with screenshots when ready).
 */
/** Single anchor for every LeanIX / Ardoq / MEGA HOPEX cell (PDF + page + tests stay aligned). */
export const WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION =
  "docs/go-to-market/COMPETITIVE_LANDSCAPE.md \u00a72.1";

export type WhyArchLucidComparisonRow = {
  dimension: string;
  leanix: string;
  ardoq: string;
  megaHopex: string;
  archlucid: string;
  /** Footnote for incumbent columns ? must equal `WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION` (Vitest-enforced). */
  competitorLandscapeCitation: string;
  /** Must reference a repository evidence path (see why-archlucid-comparison.test.ts). */
  archlucidCitation: string;
  /**
   * Procurement anchor: HTTP surface customers can exercise in evaluation **plus** a marketing asset path
   * (`/marketing/...` maps to `archlucid-ui/public/marketing/...`).
   */
  evidenceAnchor: string;
};

export const WHY_ARCHLUCID_COMPARISON_ROWS: readonly WhyArchLucidComparisonRow[] = [
  {
    dimension: "AI capability",
    competitorLandscapeCitation: WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION,
    leanix: "Basic: AI-assisted survey analysis, application rationalization suggestions",
    ardoq: "Basic: change impact simulation",
    megaHopex: "Minimal: rule-based analysis",
    archlucid:
      "Multi-agent pipeline (Topology, Cost, Compliance, Critic) with explainable findings; simulator mode for CI.",
    archlucidCitation:
      "docs/go-to-market/COMPETITIVE_LANDSCAPE.md \u00a73 (Multi-agent AI pipeline); docs/V1_SCOPE.md \u00a72 Core Pilot",
    evidenceAnchor: "GET /v1/demo/explain \u00b7 /marketing/why/evidence-callout.svg",
  },
  {
    dimension: "Governance depth",
    competitorLandscapeCitation: WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION,
    leanix: "Moderate: lifecycle management, technology risk, survey workflows",
    ardoq: "Moderate: change scenarios, impact analysis",
    megaHopex: "Strong: TOGAF / ArchiMate workflow, compliance matrices",
    archlucid:
      "Approval workflows, promotions, pre-commit governance gate, segregation of duties, policy packs (V1).",
    archlucidCitation:
      "docs/go-to-market/COMPETITIVE_LANDSCAPE.md \u00a73 (Governance workflow); docs/V1_SCOPE.md \u00a72 Enterprise Controls",
    evidenceAnchor: "GET /v1/policy-packs/effective-content \u00b7 /marketing/why/evidence-callout.svg",
  },
  {
    dimension: "Audit trail",
    competitorLandscapeCitation: WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION,
    leanix: "Basic: change history on entities",
    ardoq: "Basic: change log",
    megaHopex: "Moderate: workflow audit",
    archlucid: "Typed audit event catalog, append-only SQL audit trail, searchable export (V1).",
    archlucidCitation:
      "docs/go-to-market/COMPETITIVE_LANDSCAPE.md \u00a73 (Durable audit); docs/AUDIT_COVERAGE_MATRIX.md",
    evidenceAnchor: "GET /v1/alerts \u00b7 /marketing/why/evidence-callout.svg",
  },
  {
    dimension: "Explainability",
    competitorLandscapeCitation: WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION,
    leanix: "None (recommendations are opaque)",
    ardoq: "None",
    megaHopex: "None",
    archlucid: "Structured ExplainabilityTrace per finding; aggregate run explanation and citations (V1).",
    archlucidCitation:
      "docs/go-to-market/COMPETITIVE_LANDSCAPE.md \u00a73 (Explainability trace); docs/V1_SCOPE.md",
    evidenceAnchor: "GET /v1/explain/runs/{runId}/aggregate \u00b7 /marketing/why/evidence-callout.svg",
  },
  {
    dimension: "Deployment",
    competitorLandscapeCitation: WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION,
    leanix: "SaaS-only",
    ardoq: "SaaS-only",
    megaHopex: "SaaS or on-prem",
    archlucid: "Azure-native vendor-operated SaaS reference stack; containerized local evaluation (V1).",
    archlucidCitation: "docs/adr/0020-azure-primary-platform-permanent.md; docs/V1_SCOPE.md \u00a72.4 Deployability",
    evidenceAnchor: "GET /v1/version \u00b7 /marketing/why/evidence-callout.svg",
  },
  {
    dimension: "Architecture outputs",
    competitorLandscapeCitation: WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION,
    leanix: "Inventory-centric modeling and surveys",
    ardoq: "Graph and scenario visualization",
    megaHopex: "ArchiMate / compliance matrices",
    archlucid:
      "Versioned golden manifest, replay/compare/drift, exports (Markdown/DOCX/ZIP) from committed runs (V1).",
    archlucidCitation:
      "docs/go-to-market/COMPETITIVE_LANDSCAPE.md \u00a73 (Comparison and drift; Export and reporting); docs/V1_SCOPE.md",
    evidenceAnchor: "GET /v1/artifacts/manifests/{manifestId}/bundle \u00b7 /marketing/why/evidence-callout.svg",
  },
  {
    dimension: "Multi-cloud posture (inventory vs. run-scoped review)",
    competitorLandscapeCitation: WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION,
    leanix: "Cloud-agnostic (inventory, not design)",
    ardoq: "Cloud-agnostic (inventory)",
    megaHopex: "Cloud-agnostic",
    archlucid:
      "Most ALM catalogs stay inventory-wide; ArchLucid scopes review to a committed run with manifest, findings, and exports tied to that run id (V1).",
    archlucidCitation:
      "docs/go-to-market/COMPETITIVE_LANDSCAPE.md \u00a72.1 (Multi-cloud row); docs/go-to-market/COMPETITIVE_LANDSCAPE.md \u00a73",
    evidenceAnchor: "GET /v1/architecture/run/{runId} \u00b7 /marketing/why/evidence-callout.svg",
  },
  {
    dimension: "Integration breadth (connectors vs. contract-first API)",
    competitorLandscapeCitation: WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION,
    leanix: "Extensive: 50+ connectors, REST API, Jira, ServiceNow, CMDB",
    ardoq: "Moderate: REST API, Jira, ServiceNow",
    megaHopex: "Moderate: ArchiMate exchange, REST API",
    archlucid:
      "Most ALM suites emphasize connector catalogs; ArchLucid publishes OpenAPI v1 plus generated .NET client for CI and procurement automation (V1).",
    archlucidCitation:
      "docs/go-to-market/COMPETITIVE_LANDSCAPE.md \u00a72.1 (Integration breadth row); docs/go-to-market/INTEGRATION_CATALOG.md \u00a71",
    evidenceAnchor: "GET /openapi/v1.json \u00b7 /marketing/why/evidence-callout.svg",
  },
  {
    dimension: "Decision-grade two-run explanation",
    competitorLandscapeCitation: WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION,
    leanix: "Exports and surveys; manual reconciliation between baselines",
    ardoq: "Scenario views; not a persisted golden-manifest compare API",
    megaHopex: "Workflow-centric reports; not the same shape as replayable manifest diff",
    archlucid:
      "Most ALM tools cannot return a single GET with stakeholder narrative plus structured compare payload across two committed runs; ArchLucid exposes it under /v1/explain (V1).",
    archlucidCitation: "docs/go-to-market/COMPETITIVE_LANDSCAPE.md \u00a73 (Comparison and drift); docs/EXPLANATION_SCHEMA.md",
    evidenceAnchor: "GET /v1/explain/compare/explain \u00b7 /marketing/why/evidence-callout.svg",
  },
  {
    dimension: "Proof-of-ROI JSON aligned to sponsor narrative",
    competitorLandscapeCitation: WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION,
    leanix: "Dashboards vary; seldom a run-scoped time-to-commit JSON contract",
    ardoq: "Custom analytics; not standardized for pilots",
    megaHopex: "Project metrics; not the same sponsor-facing delta shape",
    archlucid:
      "Most ALM tools do not ship a dedicated pilot JSON for time-to-commit, findings buckets, and audit counts on a single run id; ArchLucid does via /v1/pilots (V1).",
    archlucidCitation: "docs/API_CONTRACTS.md \u00a7Pilots; docs/go-to-market/PILOT_ROI_MODEL.md",
    evidenceAnchor: "GET /v1/pilots/runs/{runId}/pilot-run-deltas \u00b7 /marketing/why/evidence-callout.svg",
  },
  {
    dimension: "Sponsor PDF parity with Markdown body",
    competitorLandscapeCitation: WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION,
    leanix: "Print-to-PDF from views; not guaranteed single-source with narrative",
    ardoq: "Export decks; manual assembly for sponsors",
    megaHopex: "Report packs; heavy formatting workflow",
    archlucid:
      "Most ALM exports diverge from chat narrative; ArchLucid posts the same first-value Markdown through a PDF projection endpoint for email-ready sharing (V1).",
    archlucidCitation: "docs/API_CONTRACTS.md \u00a7Pilots; docs/EXECUTIVE_SPONSOR_BRIEF.md \u00a71",
    evidenceAnchor: "POST /v1/pilots/runs/{runId}/first-value-report.pdf \u00b7 /marketing/why/evidence-callout.svg",
  },
  {
    dimension: "Anonymous commit-shaped demo preview",
    competitorLandscapeCitation: WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION,
    leanix: "Guided tours; gated trials",
    ardoq: "Read-only sandboxes per vendor policy",
    megaHopex: "Training datasets; not anonymous architecture commits",
    archlucid:
      "Most ALM demos require tenant setup; ArchLucid serves a cached anonymous demo preview JSON plus marketing `/demo/preview` for procurement walkthroughs (V1).",
    archlucidCitation: "docs/DEMO_PREVIEW.md; docs/adr/0027-demo-preview-cached-anonymous-commit-page.md",
    evidenceAnchor: "GET /v1/demo/preview \u00b7 /marketing/why/evidence-callout.svg",
  },
  {
    dimension: "Operator why-archlucid telemetry snapshot",
    competitorLandscapeCitation: WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION,
    leanix: "Partner slides; not a live API bundle for evaluators",
    ardoq: "Analyst PDFs; static counters",
    megaHopex: "Workshop materials; manual assembly",
    archlucid:
      "Most ALM vendors do not publish a read-only snapshot endpoint backing an in-product proof page; ArchLucid exposes counters + demo run pointers for /why-archlucid (V1).",
    archlucidCitation: "docs/API_CONTRACTS.md \u00a7Pilots; docs/operator-shell.md",
    evidenceAnchor: "GET /v1/pilots/why-archlucid-snapshot \u00b7 /marketing/why/evidence-callout.svg",
  },
  {
    dimension: "Teams notification wiring (tenant-scoped)",
    competitorLandscapeCitation: WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION,
    leanix: "ITSM/email bridges; Teams via generic integration patterns",
    ardoq: "Webhook recipes; operator-managed",
    megaHopex: "Notification workflows inside suite",
    archlucid:
      "Most ALM catalogs omit a first-party Teams incoming-webhook registration API per tenant; ArchLucid documents and ships `/v1/integrations/teams/connections` (V1).",
    archlucidCitation:
      "docs/go-to-market/INTEGRATION_CATALOG.md \u00a71; docs/integrations/MICROSOFT_TEAMS_NOTIFICATIONS.md",
    evidenceAnchor: "GET /v1/integrations/teams/connections \u00b7 /marketing/why/evidence-callout.svg",
  },
];
