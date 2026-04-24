using ArchLucid.Application.Analysis;
using ArchLucid.Contracts.Metadata;

using FluentAssertions;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Unit tests for <see cref="ComparisonReplayCostEstimator" /> heuristics (no replay execution).
/// </summary>
[Trait("Category", "Unit")]
public sealed class ComparisonReplayCostEstimatorTests
{
    [Fact]
    public async Task TryEstimateAsync_missing_record_returns_null()
    {
        Mock<IComparisonRecordRepository> repo = new();
        repo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((ComparisonRecord?)null);
        ComparisonReplayCostEstimator sut = new(repo.Object);

        ComparisonReplayCostEstimate? result =
            await sut.TryEstimateAsync("missing", null, "artifact", false, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryEstimateAsync_end_to_end_artifact_markdown_is_low_band()
    {
        Mock<IComparisonRecordRepository> repo = new();
        repo.Setup(r => r.GetByIdAsync("c1", It.IsAny<CancellationToken>())).ReturnsAsync(
            new ComparisonRecord
            {
                ComparisonRecordId = "c1", ComparisonType = ComparisonTypes.EndToEndReplay, PayloadJson = "{}"
            });
        ComparisonReplayCostEstimator sut = new(repo.Object);

        ComparisonReplayCostEstimate? result =
            await sut.TryEstimateAsync("c1", "markdown", "artifact", false, CancellationToken.None);

        result.Should().NotBeNull();
        result.RelativeCostBand.Should().Be("low");
        result.ReplayMode.Should().Be("artifact");
    }

    [Fact]
    public async Task TryEstimateAsync_invalid_replay_mode_throws_ArgumentException()
    {
        Mock<IComparisonRecordRepository> repo = new();
        repo.Setup(r => r.GetByIdAsync("c1", It.IsAny<CancellationToken>())).ReturnsAsync(
            new ComparisonRecord { ComparisonRecordId = "c1", ComparisonType = ComparisonTypes.EndToEndReplay });
        ComparisonReplayCostEstimator sut = new(repo.Object);

        Func<Task> act = async () =>
            await sut.TryEstimateAsync("c1", null, "not-a-mode", false, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task TryEstimateAsync_persistReplay_adds_score_and_factor()
    {
        Mock<IComparisonRecordRepository> repo = new();
        repo.Setup(r => r.GetByIdAsync("c1", It.IsAny<CancellationToken>())).ReturnsAsync(
            new ComparisonRecord
            {
                ComparisonRecordId = "c1", ComparisonType = ComparisonTypes.EndToEndReplay, PayloadJson = "{}"
            });
        ComparisonReplayCostEstimator sut = new(repo.Object);

        ComparisonReplayCostEstimate? withPersist =
            await sut.TryEstimateAsync("c1", "markdown", "artifact", true, CancellationToken.None);
        ComparisonReplayCostEstimate? withoutPersist =
            await sut.TryEstimateAsync("c1", "markdown", "artifact", false, CancellationToken.None);

        withPersist.Should().NotBeNull();
        withoutPersist.Should().NotBeNull();
        withPersist.ApproximateRelativeScore.Should().BeGreaterThan(withoutPersist.ApproximateRelativeScore);
        withPersist.Factors.Should().Contain(f => f.Contains("PersistReplay", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TryEstimateAsync_large_payload_adds_factor()
    {
        Mock<IComparisonRecordRepository> repo = new();
        string largePayload = new('x', 500_001);
        repo.Setup(r => r.GetByIdAsync("c1", It.IsAny<CancellationToken>())).ReturnsAsync(
            new ComparisonRecord
            {
                ComparisonRecordId = "c1",
                ComparisonType = ComparisonTypes.EndToEndReplay,
                PayloadJson = largePayload
            });
        ComparisonReplayCostEstimator sut = new(repo.Object);

        ComparisonReplayCostEstimate? result =
            await sut.TryEstimateAsync("c1", "markdown", "artifact", false, CancellationToken.None);

        result.Should().NotBeNull();
        result.Factors.Should().Contain(f => f.Contains("Large stored payload", StringComparison.Ordinal));
    }
}
