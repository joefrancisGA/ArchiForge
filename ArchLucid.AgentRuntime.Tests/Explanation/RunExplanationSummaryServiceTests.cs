using ArchLucid.Application.Explanation;
using ArchLucid.AgentRuntime.Explanation;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Explanation;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Findings;
using ArchLucid.Decisioning.Manifest.Sections;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Provenance;
using ArchLucid.Persistence.Queries;
using ArchLucid.Provenance;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.AgentRuntime.Tests.Explanation;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class RunExplanationSummaryServiceTests
{
    private static ManifestIssue Issue(string severity)
    {
        return new ManifestIssue { IssueType = "Test", Title = "t", Description = "d", Severity = severity };
    }

    [Fact]
    public void BuildThemeSummaries_groups_decision_key_drivers_by_category_and_collects_other_lines()
    {
        List<string> drivers =
        [
            "Cost: Pick SKU → A",
            "Cost: Pick region → East",
            "Security: TLS → 1.3",
            "3 topology resource(s) recorded."
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
            UnresolvedIssues = new UnresolvedIssuesSection { Items = [Issue("Critical")] }
        };

        RunExplanationSummaryService.DeriveRiskPosture(manifest).Should().Be("Critical");
    }

    [Fact]
    public void DeriveRiskPosture_medium_issue_is_Medium()
    {
        GoldenManifest manifest = new()
        {
            UnresolvedIssues = new UnresolvedIssuesSection { Items = [Issue("Medium")] }
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
                Items = [Issue("Low"), Issue("Critical"), Issue("Medium")]
            }
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
        Mock<IDeterministicExplanationService> deterministic = new();
        Mock<IProvenanceSnapshotRepository> provenance = new();
        Mock<IExplanationFaithfulnessChecker> faithfulness = new();

        RunExplanationSummaryService svc = new(
            explanation.Object,
            deterministic.Object,
            query.Object,
            provenance.Object,
            faithfulness.Object,
            Options.Create(new RunExplanationAggregateOptions()),
            NullLogger<RunExplanationSummaryService>.Instance);

        RunExplanationSummary? result = await svc.GetSummaryAsync(
            new ScopeContext { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() },
            runId,
            CancellationToken.None);

        result.Should().BeNull();
        explanation.Verify(
            e => e.ExplainRunAsync(It.IsAny<GoldenManifest>(), It.IsAny<DecisionProvenanceGraph?>(),
                It.IsAny<CancellationToken>()),
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
            Decisions =
                [new ResolvedArchitectureDecision { Category = "Cost", Title = "SKU", SelectedOption = "A" }],
            Compliance = new ComplianceSection { Gaps = ["g1"] }
        };

        ExplanationResult explained = new()
        {
            Summary = "Exec summary.",
            KeyDrivers = ["Cost: SKU → A"],
            Structured = new StructuredExplanation { Reasoning = "Body", Confidence = 0.82m },
            Confidence = 0.82m
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
                                Rationale = "r"
                            },
                            new Finding
                            {
                                FindingType = "t",
                                Category = "c",
                                EngineType = "e",
                                Title = "f2",
                                Rationale = "r"
                            }
                        ]
                    }
                });

        Mock<IExplanationService> explanation = new();
        explanation
            .Setup(e => e.ExplainRunAsync(manifest, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(explained);

        Mock<IDeterministicExplanationService> deterministic = new();

        Mock<IProvenanceSnapshotRepository> provenance = new();
        provenance
            .Setup(p => p.GetByRunIdAsync(It.IsAny<ScopeContext>(), runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DecisionProvenanceSnapshot?)null);

        Mock<IExplanationFaithfulnessChecker> faithfulness = new();
        faithfulness
            .Setup(f => f.CheckFaithfulness(It.IsAny<ExplanationResult>(), It.IsAny<FindingsSnapshot?>()))
            .Returns(new ExplanationFaithfulnessReport(1, 1, 0, 1.0, []));

        RunExplanationSummaryService svc = new(
            explanation.Object,
            deterministic.Object,
            query.Object,
            provenance.Object,
            faithfulness.Object,
            Options.Create(new RunExplanationAggregateOptions()),
            NullLogger<RunExplanationSummaryService>.Instance);

        RunExplanationSummary? summary = await svc.GetSummaryAsync(
            new ScopeContext
            {
                TenantId = manifest.TenantId, WorkspaceId = manifest.WorkspaceId, ProjectId = manifest.ProjectId
            },
            runId,
            CancellationToken.None);

        summary.Should().NotBeNull();
        summary.Explanation.Confidence.Should().Be(0.82m);
        summary.Explanation.Structured!.Confidence.Should().Be(0.82m);
        summary.FaithfulnessSupportRatio.Should().Be(1.0);
        summary.FindingTraceConfidences.Should().NotBeNull();
        summary.FindingTraceConfidences!.Count.Should().Be(2);
        summary.UsedDeterministicFallback.Should().BeFalse();
        summary.FindingCount.Should().Be(2);
        summary.DecisionCount.Should().Be(1);
        summary.UnresolvedIssueCount.Should().Be(0);
        summary.ComplianceGapCount.Should().Be(1);
        summary.RiskPosture.Should().Be("Low");
        summary.ThemeSummaries.Should().Contain(t => t.StartsWith("Cost:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetSummaryAsync_swaps_to_deterministic_when_faithfulness_support_ratio_is_low()
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
            Metadata = new ManifestMetadata { Summary = "Manifest headline" },
            UnresolvedIssues = new UnresolvedIssuesSection(),
            Decisions =
            [
                new ResolvedArchitectureDecision { Category = "Cost", Title = "SKU", SelectedOption = "A" }
            ],
            Compliance = new ComplianceSection()
        };

        ExplanationResult llmLayer = new()
        {
            Summary = "Hallucinated summary not grounded in findings.",
            DetailedNarrative = "Narrative without overlap.",
            KeyDrivers = ["Cost: SKU → A"]
        };

        ExplanationResult deterministicLayer = new()
        {
            Summary = "Deterministic headline",
            DetailedNarrative = "Deterministic body from manifest signals.",
            KeyDrivers = ["Cost: SKU → A"],
            Structured = new StructuredExplanation
            {
                SchemaVersion = 1, Reasoning = "Deterministic body from manifest signals."
            }
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
                                Title = "token-one",
                                Rationale = "alpha"
                            }
                        ]
                    }
                });

        Mock<IExplanationService> explanation = new();
        explanation
            .Setup(e => e.ExplainRunAsync(manifest, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmLayer);

        Mock<IDeterministicExplanationService> deterministic = new();
        deterministic
            .Setup(d => d.ExtractRunKeyDrivers(manifest, null))
            .Returns(["Cost: SKU → A"]);
        deterministic
            .Setup(d => d.ExtractRiskImplications(manifest))
            .Returns(["No unresolved issues recorded."]);
        deterministic
            .Setup(d => d.ExtractCostImplications(manifest))
            .Returns(["Max monthly cost not specified."]);
        deterministic
            .Setup(d => d.ExtractComplianceImplications(manifest))
            .Returns(["No compliance gaps listed."]);
        deterministic
            .Setup(d =>
                d.BuildRunExplanationFromLlmPayload(
                    manifest,
                    It.IsAny<List<string>>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<List<string>>(),
                    string.Empty))
            .Returns(deterministicLayer);

        Mock<IProvenanceSnapshotRepository> provenance = new();
        provenance
            .Setup(p => p.GetByRunIdAsync(It.IsAny<ScopeContext>(), runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DecisionProvenanceSnapshot?)null);

        Mock<IExplanationFaithfulnessChecker> faithfulness = new();
        faithfulness
            .Setup(f => f.CheckFaithfulness(llmLayer, It.IsAny<FindingsSnapshot?>()))
            .Returns(new ExplanationFaithfulnessReport(2, 2, 0, 0.05, []));

        RunExplanationAggregateOptions opts = new()
        {
            FaithfulnessFallbackEnabled = true, MinSupportRatioToTrustLlmNarrative = 0.2
        };

        RunExplanationSummaryService svc = new(
            explanation.Object,
            deterministic.Object,
            query.Object,
            provenance.Object,
            faithfulness.Object,
            Options.Create(opts),
            NullLogger<RunExplanationSummaryService>.Instance);

        RunExplanationSummary? summary = await svc.GetSummaryAsync(
            new ScopeContext
            {
                TenantId = manifest.TenantId, WorkspaceId = manifest.WorkspaceId, ProjectId = manifest.ProjectId
            },
            runId,
            CancellationToken.None);

        summary.Should().NotBeNull();
        summary.Explanation.Summary.Should().Be("Deterministic headline");
        summary.Explanation.Provenance.Should().BeNull();
        summary.Explanation.Confidence.Should().BeNull();
        summary.UsedDeterministicFallback.Should().BeTrue();
        summary.FaithfulnessSupportRatio.Should().BeApproximately(0.05, 1e-6);
        summary.FaithfulnessWarning.Should().NotBeNull();
        summary.FaithfulnessWarning.Should().Contain("deterministic");
        deterministic.Verify(
            d => d.BuildRunExplanationFromLlmPayload(
                manifest,
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                string.Empty),
            Times.Once);
    }
}
