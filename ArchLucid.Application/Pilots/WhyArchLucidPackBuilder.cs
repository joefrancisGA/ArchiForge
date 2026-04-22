using System.Text;

namespace ArchLucid.Application.Pilots;

/// <summary>
/// Builds the canonical Markdown body for the public <c>GET /v1/marketing/why-archlucid-pack.pdf</c> bundle:
/// demo-seed run excerpts (caller-supplied fragments) plus an incumbent side-by-side scaffold whose
/// **competitor** cells are explicitly tied to <c>docs/go-to-market/COMPETITIVE_LANDSCAPE.md</c> §2.1.
/// </summary>
public static class WhyArchLucidPackBuilder
{
    private const string CompetitiveLandscapePath = "docs/go-to-market/COMPETITIVE_LANDSCAPE.md";

    /// <summary>Footnote applied to LeanIX / Ardoq / MEGA columns — no uncited competitive claims.</summary>
    private const string CompetitorColumnSource =
        "Paraphrase only — see **" + CompetitiveLandscapePath + " §2.1** (public positioning matrix).";

    /// <summary>Assembles the full Markdown document passed to <see cref="WhyArchLucidPackPdfBuilder"/>.</summary>
    public static string BuildMarkdown(WhyArchLucidPackSourceDto source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        StringBuilder body = new();

        body.AppendLine("# ArchLucid — side-by-side proof pack (demo)");
        body.AppendLine();
        body.AppendLine("> **Panel banner:** *demo tenant — replace before publishing*");
        body.AppendLine();
        body.AppendLine("This pack is generated from the **cached anonymous** demo commit preview (`GET /v1/demo/preview`). It contains **no tenant-specific production data**.");
        body.AppendLine();
        body.AppendLine("---");
        body.AppendLine();
        body.AppendLine("## ArchLucid run package (deterministic demo)");
        body.AppendLine();
        body.AppendLine($"**Run id:** `{source.RunId}`  ");
        body.AppendLine($"**Project:** `{source.ProjectId}`  ");
        body.AppendLine();
        body.AppendLine("### Manifest summary");
        body.AppendLine();
        body.AppendLine(source.ManifestSectionMarkdown.Trim());
        body.AppendLine();
        body.AppendLine("### Authority chain (ids)");
        body.AppendLine();
        body.AppendLine(source.AuthorityChainSectionMarkdown.Trim());
        body.AppendLine();
        body.AppendLine("### Artifacts");
        body.AppendLine();
        body.AppendLine(source.ArtifactsSectionMarkdown.Trim());
        body.AppendLine();
        body.AppendLine("### Pipeline timeline (excerpt)");
        body.AppendLine();
        body.AppendLine(source.PipelineTimelineSectionMarkdown.Trim());
        body.AppendLine();
        body.AppendLine("### Aggregate run explanation (excerpt)");
        body.AppendLine();
        body.AppendLine(source.RunExplanationSectionMarkdown.Trim());
        body.AppendLine();
        body.AppendLine("### Citations (persisted evidence)");
        body.AppendLine();
        body.AppendLine(source.CitationsSectionMarkdown.Trim());
        body.AppendLine();
        body.AppendLine("### Comparison / drift sample (themes)");
        body.AppendLine();
        body.AppendLine(source.ComparisonDeltaSampleMarkdown.Trim());
        body.AppendLine();
        body.AppendLine("---");
        body.AppendLine();
        body.AppendLine("## Incumbent scaffold (same dimensions — sourced)");
        body.AppendLine();
        body.AppendLine(
            "The table below **does not assert vendor internals**. Competitor cells are short paraphrases of the **EAM incumbent matrix** in `"
            + CompetitiveLandscapePath
            + "` **§2.1** only. The ArchLucid column repeats the **same citations** as the public `/why` marketing table (`archlucid-ui/src/marketing/why-archlucid-comparison.ts`).");
        body.AppendLine();
        body.AppendLine(BuildIncumbentMarkdownTable());
        body.AppendLine();
        body.AppendLine("**Footnote — competitor columns:** " + CompetitorColumnSource);
        body.AppendLine();

        return body.ToString();
    }

    /// <summary>GFM pipe table aligned with <c>WHY_ARCHLUCID_COMPARISON_ROWS</c> (keep in sync when rows change).</summary>
    private static string BuildIncumbentMarkdownTable()
    {
        // Rows mirror why-archlucid-comparison.ts; ArchLucid column citations must stay repo-path anchored.
        (string Dimension, string Leanix, string Ardoq, string Mega, string Archlucid, string ArchCitation, string EvidenceAnchor)[] rows =
        [
            (
                "AI capability",
                "Basic: AI-assisted survey analysis, application rationalization suggestions",
                "Basic: change impact simulation",
                "Minimal: rule-based analysis",
                "Multi-agent pipeline (Topology, Cost, Compliance, Critic) with explainable findings; simulator mode for CI.",
                "docs/go-to-market/COMPETITIVE_LANDSCAPE.md §3 (Multi-agent AI pipeline); docs/V1_SCOPE.md §2 Core Pilot",
                "GET /v1/demo/explain · /marketing/why/evidence-callout.svg"),
            (
                "Governance depth",
                "Moderate: lifecycle management, technology risk, survey workflows",
                "Moderate: change scenarios, impact analysis",
                "Strong: TOGAF / ArchiMate workflow, compliance matrices",
                "Approval workflows, promotions, pre-commit governance gate, segregation of duties, policy packs (V1).",
                "docs/go-to-market/COMPETITIVE_LANDSCAPE.md §3 (Governance workflow); docs/V1_SCOPE.md §2 Enterprise Controls",
                "GET /v1/policy-packs/effective-content · /marketing/why/evidence-callout.svg"),
            (
                "Audit trail",
                "Basic: change history on entities",
                "Basic: change log",
                "Moderate: workflow audit",
                "Typed audit event catalog, append-only SQL audit trail, searchable export (V1).",
                "docs/go-to-market/COMPETITIVE_LANDSCAPE.md §3 (Durable audit); docs/AUDIT_COVERAGE_MATRIX.md",
                "GET /v1/alerts · /marketing/why/evidence-callout.svg"),
            (
                "Explainability",
                "None (recommendations are opaque)",
                "None",
                "None",
                "Structured ExplainabilityTrace per finding; aggregate run explanation and citations (V1).",
                "docs/go-to-market/COMPETITIVE_LANDSCAPE.md §3 (Explainability trace); docs/V1_SCOPE.md",
                "GET /v1/explain/runs/{runId}/aggregate · /marketing/why/evidence-callout.svg"),
            (
                "Deployment",
                "SaaS-only",
                "SaaS-only",
                "SaaS or on-prem",
                "Azure-native vendor-operated SaaS reference stack; containerized local evaluation (V1).",
                "docs/adr/0020-azure-primary-platform-permanent.md; docs/V1_SCOPE.md §2.4 Deployability",
                "GET /v1/version · /marketing/why/evidence-callout.svg"),
            (
                "Architecture outputs",
                "Inventory-centric modeling and surveys",
                "Graph and scenario visualization",
                "ArchiMate / compliance matrices",
                "Versioned golden manifest, replay/compare/drift, exports (Markdown/DOCX/ZIP) from committed runs (V1).",
                "docs/go-to-market/COMPETITIVE_LANDSCAPE.md §3 (Comparison and drift; Export and reporting); docs/V1_SCOPE.md",
                "GET /v1/artifacts/manifests/{manifestId}/bundle · /marketing/why/evidence-callout.svg"),
            (
                "Multi-cloud posture (inventory vs. run-scoped review)",
                "Cloud-agnostic (inventory, not design)",
                "Cloud-agnostic (inventory)",
                "Cloud-agnostic",
                "Most ALM catalogs stay inventory-wide; ArchLucid scopes review to a committed run with manifest, findings, and exports tied to that run id (V1).",
                "docs/go-to-market/COMPETITIVE_LANDSCAPE.md §2.1 (Multi-cloud row); docs/go-to-market/COMPETITIVE_LANDSCAPE.md §3",
                "GET /v1/architecture/run/{runId} · /marketing/why/evidence-callout.svg"),
            (
                "Integration breadth (connectors vs. contract-first API)",
                "Extensive: 50+ connectors, REST API, Jira, ServiceNow, CMDB",
                "Moderate: REST API, Jira, ServiceNow",
                "Moderate: ArchiMate exchange, REST API",
                "Most ALM suites emphasize connector catalogs; ArchLucid publishes OpenAPI v1 plus generated .NET client for CI and procurement automation (V1).",
                "docs/go-to-market/COMPETITIVE_LANDSCAPE.md §2.1 (Integration breadth row); docs/go-to-market/INTEGRATION_CATALOG.md §1",
                "GET /openapi/v1.json · /marketing/why/evidence-callout.svg"),
            (
                "Decision-grade two-run explanation",
                "Exports and surveys; manual reconciliation between baselines",
                "Scenario views; not a persisted golden-manifest compare API",
                "Workflow-centric reports; not the same shape as replayable manifest diff",
                "Most ALM tools cannot return a single GET with stakeholder narrative plus structured compare payload across two committed runs; ArchLucid exposes it under /v1/explain (V1).",
                "docs/go-to-market/COMPETITIVE_LANDSCAPE.md §3 (Comparison and drift); docs/EXPLANATION_SCHEMA.md",
                "GET /v1/explain/compare/explain · /marketing/why/evidence-callout.svg"),
            (
                "Proof-of-ROI JSON aligned to sponsor narrative",
                "Dashboards vary; seldom a run-scoped time-to-commit JSON contract",
                "Custom analytics; not standardized for pilots",
                "Project metrics; not the same sponsor-facing delta shape",
                "Most ALM tools do not ship a dedicated pilot JSON for time-to-commit, findings buckets, and audit counts on a single run id; ArchLucid does via /v1/pilots (V1).",
                "docs/API_CONTRACTS.md §Pilots; docs/go-to-market/PILOT_ROI_MODEL.md",
                "GET /v1/pilots/runs/{runId}/pilot-run-deltas · /marketing/why/evidence-callout.svg"),
            (
                "Sponsor PDF parity with Markdown body",
                "Print-to-PDF from views; not guaranteed single-source with narrative",
                "Export decks; manual assembly for sponsors",
                "Report packs; heavy formatting workflow",
                "Most ALM exports diverge from chat narrative; ArchLucid posts the same first-value Markdown through a PDF projection endpoint for email-ready sharing (V1).",
                "docs/API_CONTRACTS.md §Pilots; docs/EXECUTIVE_SPONSOR_BRIEF.md §1",
                "POST /v1/pilots/runs/{runId}/first-value-report.pdf · /marketing/why/evidence-callout.svg"),
            (
                "Anonymous commit-shaped demo preview",
                "Guided tours; gated trials",
                "Read-only sandboxes per vendor policy",
                "Training datasets; not anonymous architecture commits",
                "Most ALM demos require tenant setup; ArchLucid serves a cached anonymous demo preview JSON plus marketing `/demo/preview` for procurement walkthroughs (V1).",
                "docs/DEMO_PREVIEW.md; docs/adr/0027-demo-preview-cached-anonymous-commit-page.md",
                "GET /v1/demo/preview · /marketing/why/evidence-callout.svg"),
            (
                "Operator why-archlucid telemetry snapshot",
                "Partner slides; not a live API bundle for evaluators",
                "Analyst PDFs; static counters",
                "Workshop materials; manual assembly",
                "Most ALM vendors do not publish a read-only snapshot endpoint backing an in-product proof page; ArchLucid exposes counters + demo run pointers for /why-archlucid (V1).",
                "docs/API_CONTRACTS.md §Pilots; docs/operator-shell.md",
                "GET /v1/pilots/why-archlucid-snapshot · /marketing/why/evidence-callout.svg"),
            (
                "Teams notification wiring (tenant-scoped)",
                "ITSM/email bridges; Teams via generic integration patterns",
                "Webhook recipes; operator-managed",
                "Notification workflows inside suite",
                "Most ALM catalogs omit a first-party Teams incoming-webhook registration API per tenant; ArchLucid documents and ships `/v1/integrations/teams/connections` (V1).",
                "docs/go-to-market/INTEGRATION_CATALOG.md §1; docs/integrations/MICROSOFT_TEAMS_NOTIFICATIONS.md",
                "GET /v1/integrations/teams/connections · /marketing/why/evidence-callout.svg"),
        ];

        StringBuilder t = new();
        t.AppendLine("| Dimension | LeanIX (§2.1 paraphrase) | Ardoq (§2.1 paraphrase) | MEGA HOPEX (§2.1 paraphrase) | ArchLucid (V1; cited) | ArchLucid citations | Evidence anchor |");
        t.AppendLine("|-----------|--------------------------|-------------------------|------------------------------|----------------------|----------------------|-----------------|");

        foreach ((string dimension, string leanix, string ardoq, string mega, string archlucid, string archCitation, string evidenceAnchor) in rows)
        {
            t.AppendLine(
                $"| {EscapePipe(dimension)} | {EscapePipe(leanix)} | {EscapePipe(ardoq)} | {EscapePipe(mega)} | {EscapePipe(archlucid)} | {EscapePipe(archCitation)} | {EscapePipe(evidenceAnchor)} |");
        }

        return t.ToString();
    }

    private static string EscapePipe(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);
}
