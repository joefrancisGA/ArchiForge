using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Persistence.Queries;

using Moq;

namespace ArchLucid.Persistence.Tests.Compare;

public sealed class AuthorityCompareServiceCompareRunsTests
{
    [Fact]
    public async Task
        CompareRunsAsync_omits_manifest_comparison_when_only_one_run_has_golden_manifest_but_run_diff_shows_asymmetry()
    {
        Guid leftRunId = Guid.NewGuid();
        Guid rightRunId = Guid.NewGuid();
        Guid manifestId = Guid.NewGuid();
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid()
        };

        RunSummaryDto left = new()
        {
            RunId = leftRunId,
            ProjectId = "default",
            Description = null,
            GoldenManifestId = manifestId
        };

        RunSummaryDto right = new()
        {
            RunId = rightRunId,
            ProjectId = "default",
            Description = null,
            GoldenManifestId = null
        };

        Mock<IGoldenManifestRepository> manifests = new();
        Mock<IAuthorityQueryService> queries = new();
        queries.Setup(q => q.GetRunSummaryAsync(scope, leftRunId, It.IsAny<CancellationToken>())).ReturnsAsync(left);
        queries.Setup(q => q.GetRunSummaryAsync(scope, rightRunId, It.IsAny<CancellationToken>())).ReturnsAsync(right);

        AuthorityCompareService sut = new(manifests.Object, queries.Object);

        RunComparisonResult? result = await sut.CompareRunsAsync(scope, leftRunId, rightRunId, CancellationToken.None);

        result.Should().NotBeNull();
        result.ManifestComparison.Should().BeNull();

        DiffItem? goldenDiff = result.RunLevelDiffs.SingleOrDefault(d => d.Key == "GoldenManifestId");
        goldenDiff.Should().NotBeNull();
        goldenDiff.DiffKind.Should().Be(DiffKind.Changed);
        goldenDiff.BeforeValue.Should().Be(manifestId.ToString());
        goldenDiff.AfterValue.Should().BeNull();
    }
}
