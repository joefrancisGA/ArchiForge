using ArchLucid.AgentRuntime.Explanation;
using ArchLucid.Core.Explanation;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Manifest.Sections;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Provenance;
using ArchLucid.Persistence.Queries;
using ArchLucid.Provenance;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.AgentRuntime.Tests.Explanation;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class RunExplanationSummaryServiceTests
{
    private static ManifestIssue Issue(string severity) => new()
    {
        IssueType = "Test",
        Title = "t",
        Description = "d",
        Severity = severity,
    };

    [Fact]
    public void BuildThemeSummaries_groups_decision_key_drivers_by_category_and_collects_other_lines()
    {
        List<string> drivers =
        [
            "Cost: Pick SKU → A",
            "Cost: Pick region → East",
            "Security: TLS → 1.3",
            "3 topology resource(s) recorded.",
        ];

        List<string> themes = RunExplanationSummaryService.BuildThemeSummaries(drivers);

        themes.Should().Contain(t => t.StartsWith("Cost:", StringComparison.Ordinal) && t.Contains("2 key driver"));
        themes.Should().Contain(t => t.StartsWith("Security:", StringComparison.Ordinal));
        themes.Should().Contain(t => t.Contains("Additional signals:", StringComparison.Ordinal));
    }

    [Fact]
    public void TryParseDecisionDriverLine_recognizes_explanation_service_key_driver_shape()
    {
        bool ok = RunExplanationSummaryService.TryParseDecisionDriverLine(
            "Reliability: Use multi-AZ → enabled",
            out string category,
            out string rest);

        ok.Should().BeTrue();
        category.Should().Be("Reliability");
        rest.Should().Be("Use multi-AZ → enabled");
    }

    [Fact]
    public void DeriveRiskPosture_no_unresolved_issues_is_Low()
    {
        GoldenManifest manifest = new() { UnresolvedIssues = new UnresolvedIssuesSection() };

        RunExplanationSummaryService.DeriveRiskPosture(manifest).Should().Be("Low");
    }

    [Fact]
    public void DeriveRiskPosture_critical_issue_is_Critical()
    {
        GoldenManifest manifest = new()
        {
            UnresolvedIssues = new UnresolvedIssuesSection
            {
                Items = [Issue("Critical")],
            },
        };

        RunExplanationSummaryService.DeriveRiskPosture(manifest).Should().Be("Critical");
    }

    [Fact]
    public void DeriveRiskPosture_medium_issue_is_Medium()
    {
        GoldenManifest manifest = new()
        {
            UnresolvedIssues = new UnresolvedIssuesSection
            {
                Items = [Issue("Medium")],
            },
        };

        RunExplanationSummaryService.DeriveRiskPosture(manifest).Should().Be("Medium");
    }

    [Fact]
    public void DeriveRiskPosture_mixed_severities_picks_highest()
    {
        GoldenManifest manifest = new()
        {
            UnresolvedIssues = new UnresolvedIssuesSection
            {
                Items = [Issue("Low"), Issue("Critical"), Issue("Medium")],
            },
        };

        RunExplanationSummaryService.DeriveRiskPosture(manifest).Should().Be("Critical");
    }

    [Fact]
    public async Task GetSummaryAsync_returns_null_when_run_detail_missing()
    {
        Guid runId = Guid.NewGuid();
        Mock<IAuthorityQueryService> query = new();
        query
            .Setup(q => q.GetRunDetailAsync(It.IsAny<ScopeContext>(), runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunDetailDto?)null);

        Mock<IExplanationService> explanation = new();
        Mock<IProvenanceSnapshotRepository> provenance = new();

        RunExplanationSummaryService svc = new(
            explanation.Object,
            query.Object,
            provenance.Object,
            NullLogger<RunExplanationSummaryService>.Instance);

        RunExplanationSummary? result = await svc.GetSummaryAsync(
            new ScopeContext { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() },
            runId,
            CancellationToken.None);

        result.Should().BeNull();
        explanation.Verify(
            e => e.ExplainRunAsync(It.IsAny<GoldenManifest>(), It.IsAny<DecisionProvenanceGraph?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetSummaryAsync_surfaces_explanation_confidence_and_counts()
    {
        Guid runId = Guid.NewGuid();
        GoldenManifest manifest = new()
        {
            RunId = runId,
            ManifestId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            ContextSnapshotId = Guid.NewGuid(),
            GraphSnapshotId = Guid.NewGuid(),
            FindingsSnapshotId = Guid.NewGuid(),
            DecisionTraceId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow,
            ManifestHash = "h",
            RuleSetId = "rs",
            RuleSetVersion = "1",
            RuleSetHash = "rsh",
            Metadata = new ManifestMetadata(),
            UnresolvedIssues = new UnresolvedIssuesSection(),
            Decisions = [new ResolvedArchitectureDecision { Category = "Cost", Title = "SKU", SelectedOption = "A" }],
            Compliance = new ComplianceSection { Gaps = ["g1"] },
        };

        ExplanationResult explained = new()
        {
            Summary = "Exec summary.",
            KeyDrivers = ["Cost: SKU → A"],
            Structured = new StructuredExplanation
            {
                Reasoning = "Body",
                Confidence = 0.82m,
            },
            Confidence = 0.82m,
        };

        Mock<IAuthorityQueryService> query = new();
        query
            .Setup(q => q.GetRunDetailAsync(It.IsAny<ScopeContext>(), runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new RunDetailDto
                {
                    Run = new RunRecord { RunId = runId, TenantId = manifest.TenantId },
                    GoldenManifest = manifest,
                    FindingsSnapshot = new FindingsSnapshot
                    {
                        Findings =
                        [
                            new Finding
                            {
                                FindingType = "t",
                                Category = "c",
                                EngineType = "e",
                                Title = "f1",
                                Rationale = "r",
                            },
                            new Finding
                            {
                                FindingType = "t",
                                Category = "c",
                                EngineType = "e",
                                Title = "f2",
                                Rationale = "r",
                            },
                        ],
                    },
                });

        Mock<IExplanationService> explanation = new();
        explanation
            .Setup(e => e.ExplainRunAsync(manifest, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(explained);

        Mock<IProvenanceSnapshotRepository> provenance = new();
        provenance
            .Setup(p => p.GetByRunIdAsync(It.IsAny<ScopeContext>(), runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DecisionProvenanceSnapshot?)null);

        RunExplanationSummaryService svc = new(
            explanation.Object,
            query.Object,
            provenance.Object,
            NullLogger<RunExplanationSummaryService>.Instance);

        RunExplanationSummary? summary = await svc.GetSummaryAsync(
            new ScopeContext
            {
                TenantId = manifest.TenantId,
                WorkspaceId = manifest.WorkspaceId,
                ProjectId = manifest.ProjectId,
            },
            runId,
            CancellationToken.None);

        summary.Should().NotBeNull();
        summary!.Explanation.Confidence.Should().Be(0.82m);
        summary.Explanation.Structured!.Confidence.Should().Be(0.82m);
        summary.FindingCount.Should().Be(2);
        summary.DecisionCount.Should().Be(1);
        summary.UnresolvedIssueCount.Should().Be(0);
        summary.ComplianceGapCount.Should().Be(1);
        summary.RiskPosture.Should().Be("Low");
        summary.ThemeSummaries.Should().Contain(t => t.StartsWith("Cost:", StringComparison.Ordinal));
    }
}
