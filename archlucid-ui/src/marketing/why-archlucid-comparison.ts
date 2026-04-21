/**
 * Head-to-head rows for the public /why page. Competitor cells summarize
 * docs/go-to-market/COMPETITIVE_LANDSCAPE.md §2.1. ArchLucid cells summarize
 * docs/go-to-market/COMPETITIVE_LANDSCAPE.md §3 with repo-path citations only.
 */
/** Single anchor for every LeanIX / Ardoq / MEGA HOPEX cell (PDF + page + tests stay aligned). */
export const WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION = "docs/go-to-market/COMPETITIVE_LANDSCAPE.md §2.1";

export type WhyArchlucidComparisonRow = {
  dimension: string;
  leanix: string;
  ardoq: string;
  megaHopex: string;
  archlucid: string;
  /** Footnote for incumbent columns — must equal `WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION` (Vitest-enforced). */
  competitorLandscapeCitation: string;
  /** Must reference a repository evidence path (see why-archlucid-comparison.test.ts). */
  archlucidCitation: string;
};

export const WHY_ARCHLUCID_COMPARISON_ROWS: readonly WhyArchlucidComparisonRow[] = [
  {
    dimension: "AI capability",
    competitorLandscapeCitation: WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION,
    leanix: "Basic: AI-assisted survey analysis, application rationalization suggestions",
    ardoq: "Basic: change impact simulation",
    megaHopex: "Minimal: rule-based analysis",
    archlucid:
      "Multi-agent pipeline (Topology, Cost, Compliance, Critic) with explainable findings; simulator mode for CI.",
    archlucidCitation:
      "docs/go-to-market/COMPETITIVE_LANDSCAPE.md §3 (Multi-agent AI pipeline); docs/V1_SCOPE.md §2 Core Pilot",
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
      "docs/go-to-market/COMPETITIVE_LANDSCAPE.md §3 (Governance workflow); docs/V1_SCOPE.md §2 Enterprise Controls",
  },
  {
    dimension: "Audit trail",
    competitorLandscapeCitation: WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION,
    leanix: "Basic: change history on entities",
    ardoq: "Basic: change log",
    megaHopex: "Moderate: workflow audit",
    archlucid: "Typed audit event catalog, append-only SQL audit trail, searchable export (V1).",
    archlucidCitation:
      "docs/go-to-market/COMPETITIVE_LANDSCAPE.md §3 (Durable audit); docs/AUDIT_COVERAGE_MATRIX.md",
  },
  {
    dimension: "Explainability",
    competitorLandscapeCitation: WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION,
    leanix: "None (recommendations are opaque)",
    ardoq: "None",
    megaHopex: "None",
    archlucid: "Structured ExplainabilityTrace per finding; aggregate run explanation and citations (V1).",
    archlucidCitation:
      "docs/go-to-market/COMPETITIVE_LANDSCAPE.md §3 (Explainability trace); docs/V1_SCOPE.md",
  },
  {
    dimension: "Deployment",
    competitorLandscapeCitation: WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION,
    leanix: "SaaS-only",
    ardoq: "SaaS-only",
    megaHopex: "SaaS or on-prem",
    archlucid: "Azure-native vendor-operated SaaS reference stack; containerized local evaluation (V1).",
    archlucidCitation: "docs/adr/0020-azure-primary-platform-permanent.md; docs/V1_SCOPE.md §2.4 Deployability",
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
      "docs/go-to-market/COMPETITIVE_LANDSCAPE.md §3 (Comparison and drift; Export and reporting); docs/V1_SCOPE.md",
  },
];
