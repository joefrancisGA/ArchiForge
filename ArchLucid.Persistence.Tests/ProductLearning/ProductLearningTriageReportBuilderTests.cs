using ArchLucid.Contracts.ProductLearning;
using ArchLucid.Persistence.Coordination.ProductLearning;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.ProductLearning;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
[Trait("ChangeSet", "58R")]
public sealed class ProductLearningTriageReportBuilderTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void Build_and_Format_include_core_sections_and_totals()
    {
        DateTime utc = new(2026, 4, 2, 12, 0, 0, DateTimeKind.Utc);

        LearningDashboardSummary summary = new()
        {
            GeneratedUtc = utc,
            TenantId = TenantId,
            WorkspaceId = TenantId,
            ProjectId = TenantId,
            TotalSignalsInScope = 5,
            DistinctRunsTouched = 2,
            ArtifactTrends =
            [
                new ArtifactOutcomeTrend
                {
                    TrendKey = "t1",
                    ArtifactTypeOrHint = "Manifest",
                    AcceptedOrTrustedCount = 1,
                    RevisionCount = 3,
                    RejectionCount = 1,
                    NeedsFollowUpCount = 0,
                    DistinctRunCount = 2,
                },
            ],
            Opportunities =
            [
                new ImprovementOpportunity
                {
                    OpportunityId = Guid.NewGuid(),
                    Title = "Improve diagram clarity",
                    Summary = "Pilots asked for clearer boundaries.",
                    Severity = "Medium",
                    PriorityRank = 1,
                    AffectedArtifactTypeOrWorkflowArea = "Diagrams",
                },
            ],
            TriageQueue =
            [
                new TriageQueueItem
                {
                    QueueItemId = Guid.NewGuid(),
                    Title = "Review rollup",
                    DetailSummary = "Pattern X",
                    PriorityRank = 1,
                    Severity = "High",
                    AffectedArtifactTypeOrWorkflowArea = "Runs",
                    TriageStatus = "Open",
                    FirstSeenUtc = utc,
                    LastSeenUtc = utc,
                    SuggestedNextAction = "Discuss in triage",
                },
            ],
        };

        ProductLearningTriageReportLimits limits = new()
        {
            MaxArtifactRows = 5,
            MaxImprovements = 5,
            MaxTriagePreview = 5,
            MaxProblemAreaLines = 4,
        };

        ProductLearningTriageReportDocument doc =
            ProductLearningTriageReportBuilder.Build(summary, limits, sinceUtc: utc);

        doc.TotalSignalsInScope.Should().Be(5);
        doc.DistinctRunsReviewed.Should().Be(2);
        doc.ArtifactOutcomes.Should().HaveCount(1);
        doc.ArtifactOutcomes[0].Revised.Should().Be(3);
        doc.TopImprovements.Should().HaveCount(1);
        doc.TriageQueuePreview.Should().HaveCount(1);

        string md = ProductLearningTriageReportMarkdownFormatter.Format(doc);

        md.Should().Contain("# Pilot feedback");
        md.Should().Contain("Runs with feedback");
        md.Should().Contain("Manifest");
        md.Should().Contain("Improve diagram clarity");
        md.Should().Contain("Triage queue");
    }

    [Fact]
    public void Build_orders_artifact_rows_by_negative_signal_mass_then_key()
    {
        DateTime utc = new(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc);

        LearningDashboardSummary summary = new()
        {
            GeneratedUtc = utc,
            TenantId = TenantId,
            WorkspaceId = TenantId,
            ProjectId = TenantId,
            ArtifactTrends =
            [
                new ArtifactOutcomeTrend
                {
                    TrendKey = "b",
                    ArtifactTypeOrHint = "Low pain",
                    RevisionCount = 0,
                    RejectionCount = 0,
                    NeedsFollowUpCount = 0,
                },
                new ArtifactOutcomeTrend
                {
                    TrendKey = "a",
                    ArtifactTypeOrHint = "High pain",
                    RevisionCount = 2,
                    RejectionCount = 1,
                    NeedsFollowUpCount = 1,
                },
            ],
        };

        ProductLearningTriageReportDocument doc = ProductLearningTriageReportBuilder.Build(
            summary,
            new ProductLearningTriageReportLimits { MaxArtifactRows = 10 },
            sinceUtc: null);

        doc.ArtifactOutcomes[0].ArtifactLabel.Should().Be("High pain");
        doc.ArtifactOutcomes[1].ArtifactLabel.Should().Be("Low pain");
    }

    [Fact]
    public void Format_markdown_documents_empty_triage_without_throwing()
    {
        DateTime utc = new(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc);

        LearningDashboardSummary summary = new()
        {
            GeneratedUtc = utc,
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
        };

        ProductLearningTriageReportDocument doc =
            ProductLearningTriageReportBuilder.Build(summary, new ProductLearningTriageReportLimits(), sinceUtc: null);

        string md = ProductLearningTriageReportMarkdownFormatter.Format(doc);

        md.Should().Contain("Triage queue");
        md.Should().Contain("Queue empty");
    }
}
