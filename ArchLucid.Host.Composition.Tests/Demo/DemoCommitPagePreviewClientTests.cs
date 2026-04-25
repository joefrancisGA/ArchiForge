using ArchLucid.AgentRuntime.Explanation;
using ArchLucid.Application.Audit;
using ArchLucid.Application.Bootstrap;
using ArchLucid.ArtifactSynthesis.Packaging;
using ArchLucid.Core.Explanation;
using ArchLucid.Core.Scoping;
using ArchLucid.Host.Core.Demo;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Queries;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Host.Composition.Tests.Demo;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class DemoCommitPagePreviewClientTests
{
    [Fact]
    public async Task GetLatestCommittedDemoCommitPageAsync_composes_all_sections_when_data_present()
    {
        Guid manifestId = Guid.NewGuid();
        RunRecord run = new()
        {
            RunId = ContosoRetailDemoIdentifiers.AuthorityRunBaselineId,
            ArchitectureRequestId = ContosoRetailDemoIdentifiers.RequestContoso,
            GoldenManifestId = manifestId,
            ProjectId = "default",
            Description = "demo",
            CreatedUtc = DateTime.UtcNow,
            ContextSnapshotId = Guid.NewGuid(),
            GraphSnapshotId = Guid.NewGuid()
        };

        Mock<IDemoSeedRunResolver> seedResolver = new();
        seedResolver.Setup(s => s.ResolveLatestCommittedDemoRunAsync(It.IsAny<CancellationToken>())).ReturnsAsync(run);

        RunDetailDto detail = new()
        {
            Run = run
        };

        ManifestSummaryDto manifestDto = new()
        {
            ManifestId = manifestId,
            RunId = run.RunId,
            CreatedUtc = DateTime.UtcNow,
            ManifestHash = "hash",
            RuleSetId = "rs",
            RuleSetVersion = "1",
            DecisionCount = 2,
            WarningCount = 1,
            UnresolvedIssueCount = 0,
            Status = "ok"
        };

        IReadOnlyList<ArtifactDescriptor> descriptors =
        [
            new()
            {
                ArtifactId = Guid.NewGuid(),
                ArtifactType = "docx",
                Name = "a",
                Format = "binary",
                CreatedUtc = DateTime.UtcNow,
                ContentHash = "h1"
            }
        ];

        IReadOnlyList<RunPipelineTimelineItemDto> timeline =
        [
            new(Guid.NewGuid(), DateTime.UtcNow, "Commit", "actor", "corr")
        ];

        RunExplanationSummary summary = new()
        {
            Explanation = new ExplanationResult { Summary = "S" },
            ThemeSummaries = ["t1"],
            OverallAssessment = "A",
            RiskPosture = "Low"
        };

        Mock<IAuthorityQueryService> authority = new();
        authority
            .Setup(a => a.GetRunDetailAsync(It.IsAny<ScopeContext>(), run.RunId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);
        authority
            .Setup(a => a.GetManifestSummaryAsync(It.IsAny<ScopeContext>(), manifestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifestDto);

        Mock<IArtifactQueryService> artifacts = new();
        artifacts
            .Setup(a => a.ListArtifactsByManifestIdAsync(It.IsAny<ScopeContext>(), manifestId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(descriptors);

        Mock<IRunPipelineAuditTimelineService> pipeline = new();
        pipeline
            .Setup(p => p.GetTimelineAsync(It.IsAny<ScopeContext>(), run.RunId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(timeline);

        Mock<IRunExplanationSummaryService> explain = new();
        explain
            .Setup(e => e.GetSummaryAsync(It.IsAny<ScopeContext>(), run.RunId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        DemoCommitPagePreviewClient sut = new(
            seedResolver.Object,
            authority.Object,
            artifacts.Object,
            pipeline.Object,
            explain.Object,
            TimeProvider.System,
            NullLogger<DemoCommitPagePreviewClient>.Instance);

        DemoCommitPagePreviewResponse? response = await sut.GetLatestCommittedDemoCommitPageAsync();

        response.Should().NotBeNull();
        response.Run.RunId.Should().Be(run.RunId.ToString("N"));
        response.Manifest.DecisionCount.Should().Be(2);
        response.AuthorityChain.ContextSnapshotId.Should().NotBeNull();
        response.Artifacts.Should().HaveCount(1);
        response.PipelineTimeline.Should().HaveCount(1);
        response.RunExplanation.Should().BeSameAs(summary);
    }

    [Fact]
    public async Task GetLatestCommittedDemoCommitPageAsync_returns_null_when_resolver_has_no_run()
    {
        Mock<IDemoSeedRunResolver> seedResolver = new();
        seedResolver.Setup(s => s.ResolveLatestCommittedDemoRunAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunRecord?)null);

        DemoCommitPagePreviewClient sut = new(
            seedResolver.Object,
            new Mock<IAuthorityQueryService>().Object,
            new Mock<IArtifactQueryService>().Object,
            new Mock<IRunPipelineAuditTimelineService>().Object,
            new Mock<IRunExplanationSummaryService>().Object,
            TimeProvider.System,
            NullLogger<DemoCommitPagePreviewClient>.Instance);

        DemoCommitPagePreviewResponse? response = await sut.GetLatestCommittedDemoCommitPageAsync();

        response.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestCommittedDemoCommitPageAsync_returns_null_when_explanation_missing()
    {
        Guid manifestId = Guid.NewGuid();
        RunRecord run = new()
        {
            RunId = Guid.NewGuid(),
            ArchitectureRequestId = ContosoRetailDemoIdentifiers.RequestContoso,
            GoldenManifestId = manifestId,
            ProjectId = "default",
            CreatedUtc = DateTime.UtcNow
        };

        Mock<IDemoSeedRunResolver> seedResolver = new();
        seedResolver.Setup(s => s.ResolveLatestCommittedDemoRunAsync(It.IsAny<CancellationToken>())).ReturnsAsync(run);

        Mock<IAuthorityQueryService> authority = new();
        authority
            .Setup(a => a.GetRunDetailAsync(It.IsAny<ScopeContext>(), run.RunId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RunDetailDto { Run = run });
        authority
            .Setup(a => a.GetManifestSummaryAsync(It.IsAny<ScopeContext>(), manifestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ManifestSummaryDto
            {
                ManifestId = manifestId,
                RunId = run.RunId,
                CreatedUtc = DateTime.UtcNow,
                ManifestHash = "h",
                RuleSetId = "r",
                RuleSetVersion = "v",
                DecisionCount = 0,
                WarningCount = 0,
                UnresolvedIssueCount = 0,
                Status = "s"
            });

        Mock<IArtifactQueryService> artifacts = new();
        artifacts
            .Setup(a => a.ListArtifactsByManifestIdAsync(It.IsAny<ScopeContext>(), manifestId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Mock<IRunPipelineAuditTimelineService> pipeline = new();
        pipeline
            .Setup(p => p.GetTimelineAsync(It.IsAny<ScopeContext>(), run.RunId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Mock<IRunExplanationSummaryService> explain = new();
        explain
            .Setup(e => e.GetSummaryAsync(It.IsAny<ScopeContext>(), run.RunId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunExplanationSummary?)null);

        DemoCommitPagePreviewClient sut = new(
            seedResolver.Object,
            authority.Object,
            artifacts.Object,
            pipeline.Object,
            explain.Object,
            TimeProvider.System,
            NullLogger<DemoCommitPagePreviewClient>.Instance);

        DemoCommitPagePreviewResponse? response = await sut.GetLatestCommittedDemoCommitPageAsync();

        response.Should().BeNull();
    }
}
