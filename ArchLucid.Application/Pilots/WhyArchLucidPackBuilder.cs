using System.Text;

namespace ArchLucid.Application.Pilots;

/// <summary>
///     Builds the canonical Markdown body for the public <c>GET /v1/marketing/why-archlucid-pack.pdf</c> bundle:
///     demo-seed run excerpts (caller-supplied fragments) plus **five benchmarked differentiation rows**
///     kept in sync with <c>archlucid-ui/src/marketing/why-archlucid-comparison.ts</c>.
/// </summary>
public static class WhyArchLucidPackBuilder
{
    /// <summary>Assembles the full Markdown document passed to <see cref="WhyArchLucidPackPdfBuilder" />.</summary>
    public static string BuildMarkdown(WhyArchLucidPackSourceDto source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        StringBuilder body = new();

        body.AppendLine("# ArchLucid — side-by-side proof pack (demo)");
        body.AppendLine();
        body.AppendLine("> **Panel banner:** *demo tenant — replace before publishing*");
        body.AppendLine();
        body.AppendLine(
            "This pack is generated from the **cached anonymous** demo commit preview (`GET /v1/demo/preview`). It contains **no tenant-specific production data**.");
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
        body.AppendLine("## Benchmarked differentiation (five claims)");
        body.AppendLine();
        body.AppendLine(
            "> **Five capability claims, every claim cited to a file in this repository or to an external public source.**");
        body.AppendLine();
        body.AppendLine(
            "The Markdown table below is **byte-for-row identical** to `archlucid-ui/src/marketing/why-archlucid-comparison.ts` "
            + "(`WHY_ARCHLUCID_COMPARISON_ROWS`) — update both in one commit when claims change.");
        body.AppendLine();
        body.AppendLine(BuildDifferentiationMarkdownTable());
        body.AppendLine();

        return body.ToString();
    }

    /// <summary>GFM pipe table aligned with <c>WHY_ARCHLUCID_COMPARISON_ROWS</c> (keep in sync when rows change).</summary>
    private static string BuildDifferentiationMarkdownTable()
    {
        (string Claim, string ArchlucidEvidence, string CompetitorBaseline, string Citation, string Narrative)[] rows =
        [
            (
                "ArchLucid records **typed audit events** in SQL for mutating API work and returns **scope-filtered** listings over `GET /v1/audit` and `GET /v1/audit/search`, so reviewers can anchor evidence to the same tenant/workspace/project slice the operator UI uses.",
                "`ArchLucid.Api/Controllers/Admin/AuditController.cs` · `ArchLucid.Persistence.Audit` · `docs/library/AUDIT_COVERAGE_MATRIX.md` · `ArchLucid.Core/Audit/AuditEventTypes.cs`",
                "Incumbent diagram-and-doc stacks typically scatter decisions across wikis, tickets, and decks; **reconstructing one architecture review cycle** for a single initiative often costs **2–6 skilled hours** of manual assembly (**first-party assertion (no external citation yet)**).",
                "first-party assertion (no external citation yet)",
                "The audit controller is rate-limited and `ReadAuthority`-gated like other list surfaces, but the payload is **append-only rows** keyed to scope, not a free-form page history. The matrix doc lists the **78** event constants so procurement can map controls to rows. Together they mean \"prove what happened on this run\" is a **query**, not an archaeology sprint. Export and CSV tiers remain documented separately from this read surface."),
            (
                "ArchLucid enforces **tenant isolation at SQL Server** using `SESSION_CONTEXT`-driven row-level security policies wired through the persistence layer, not only application-layer filters.",
                "`docs/security/MULTI_TENANT_RLS.md` · `ArchLucid.Persistence.Tests/RlsArchLucidScopeIntegrationTests.cs` · SQL migrations under `ArchLucid.Persistence` (RLS objects)",
                "Multi-tenant products that rely on **per-customer schemas** or ad-hoc database splits often add **8–20 DBA/engineering hours** per new tenant for provisioning, migration, and backup policy (**first-party assertion (no external citation yet)**).",
                "https://learn.microsoft.com/en-us/sql/relational-databases/security/row-level-security?view=sql-server-ver17",
                "RLS is boring on purpose: the session context is set on connections so even an honest mistake in a repository query still cannot cross tenants. The integration tests lock the ArchLucid scope keys the API relies on. The security doc explains what is deployed versus what remains historical naming. That combination is what lets hosted SaaS teams sleep during a noisy neighbor incident."),
            (
                "Operators can enable **`ArchLucid:Governance:PreCommitGateEnabled`** so **golden manifest commits** consult governance findings and policy assignments **before** the commit succeeds, returning a structured problem response when blocked.",
                "`docs/library/PRE_COMMIT_GOVERNANCE_GATE.md` · `ArchLucid.Application/Governance/PreCommitGovernanceGate.cs` · `ArchLucid.Application.Tests/ArchitectureRunCommitPipelineIntegrationTests.cs` (gate exercised)",
                "Teams that depend on **post-merge pull-request review only** discover policy violations **after** the manifest is already treated as canonical — rework lands in **ITSM-only tools** as incident debt (**first-party assertion (no external citation yet)**).",
                "https://csrc.nist.gov/projects/ssdf",
                "The gate is opt-in because some pilots need speed first; when flipped on, the commit path calls the same governance evaluation code paths the docs describe. Integration tests prove the blocked branch emits durable audit semantics. That is a different class of safety than a comment thread checkbox."),
            (
                "CI **locks golden-cohort expected manifest fingerprints** via `GoldenCohortBaselineConstants` and `scripts/ci/assert_golden_cohort_baseline_locked.py`, and the **golden-cohort nightly workflow** exercises the cohort on a schedule separate from product unit tests.",
                "`ArchLucid.Application/GoldenCohort/GoldenCohortBaselineConstants.cs` · `scripts/ci/assert_golden_cohort_baseline_locked.py` · `.github/workflows/golden-cohort-nightly.yml` · `tests/golden-cohort/cohort.json`",
                "Manual **prompt regression review** for each model or policy change — often **half a day per release** of unstructured diff reading — is the usual substitute when no locked cohort exists (**first-party assertion (no external citation yet)**).",
                "https://github.com/joefrancisGA/ArchLucid/blob/main/.github/workflows/golden-cohort-nightly.yml",
                "The placeholder SHA constant exists so CI can fail loudly until an owner-approved baseline lock run replaces zeros with real fingerprints. The assert script is the merge-blocking guardrail; the nightly workflow is where longer cohort work runs. Together they document **deterministic drift detection** instead of vibes-based \"the model feels fine.\""),
            (
                "After commit, **`IFindingEvidenceChainService`** reconstructs explainability links for findings, and **`GET /v1/authority/runs/{runId}/provenance`** returns a **decision provenance graph** tying manifest, graph snapshot, findings snapshot, authority trace, and artifacts when the authority pipeline is complete.",
                "`ArchLucid.Application/Explanation/IFindingEvidenceChainService.cs` · `ArchLucid.Api/Controllers/Authority/AuthorityQueryController.cs` (provenance action) · `docs/library/KNOWLEDGE_GRAPH.md`",
                "Static architecture decision logs **without traversable evidence linkage** force readers to **manually open** ten attachments per finding (**first-party assertion (no external citation yet)**).",
                "https://en.wikipedia.org/wiki/Data_provenance",
                "The provenance endpoint deliberately returns **422** until the golden manifest, graph snapshot, findings snapshot, and trace exist — that honesty avoids marketing a graph that is not there. The evidence-chain service is what feeds richer explanations and pilot deltas when data is present. The knowledge-graph doc is the operator-facing map of how to read the UI graph modes.")
        ];

        StringBuilder t = new();
        t.AppendLine("| Claim | ArchLucid evidence | Competitor baseline | Citation | Narrative (≤4 sentences) |");
        t.AppendLine("|-------|--------------------|--------------------|----------|---------------------------|");

        foreach ((string claim, string archlucidEvidence, string competitorBaseline, string citation,
                     string narrative) in rows)
        {
            t.AppendLine(
                $"| {EscapePipe(claim)} | {EscapePipe(archlucidEvidence)} | {EscapePipe(competitorBaseline)} | {EscapePipe(citation)} | {EscapePipe(narrative)} |");
        }

        return t.ToString();
    }

    private static string EscapePipe(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }
}
