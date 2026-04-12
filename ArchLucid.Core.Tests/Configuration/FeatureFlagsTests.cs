using ArchLucid.Core.Configuration;
using ArchLucid.Host.Core.Authority;
using ArchLucid.Host.Core.Configuration;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;

using Moq;

namespace ArchLucid.Core.Tests.Configuration;

/// <summary>Tests for <see cref="IFeatureFlags"/> adapter and <see cref="FeatureManagementAuthorityPipelineModeResolver"/>.</summary>
[Trait("Category", "Unit")]
public sealed class FeatureFlagsTests
{
    [Fact]
    public async Task FeatureManagementFeatureFlags_IsEnabledAsync_DelegatesToFeatureManager_WithSameCancellationToken()
    {
        Mock<IFeatureManager> featureManager = new();
        CancellationToken capturedToken = default;
        featureManager
            .Setup(m => m.IsEnabledAsync("MyFeature", It.IsAny<CancellationToken>()))
            .Returns(
                (string _, CancellationToken ct) =>
                {
                    capturedToken = ct;

                    return Task.FromResult(true);
                });

        IFeatureFlags sut = new FeatureManagementFeatureFlags(featureManager.Object);
        CancellationTokenSource cts = new();

        bool enabled = await sut.IsEnabledAsync("MyFeature", cts.Token);

        enabled.Should().BeTrue();
        capturedToken.Should().Be(cts.Token);
        featureManager.Verify(
            m => m.IsEnabledAsync("MyFeature", cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task FeatureManagementFeatureFlags_IsEnabledAsync_ReturnsFalse_WhenManagerReturnsFalse()
    {
        Mock<IFeatureManager> featureManager = new();
        featureManager
            .Setup(m => m.IsEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        IFeatureFlags sut = new FeatureManagementFeatureFlags(featureManager.Object);

        bool enabled = await sut.IsEnabledAsync(AuthorityPipelineFeatureFlags.AsyncAuthorityPipeline);

        enabled.Should().BeFalse();
    }

    [Fact]
    public void FeatureManagementFeatureFlags_Constructor_ThrowsWhenFeatureManagerNull()
    {
        Action act = () => _ = new FeatureManagementFeatureFlags(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("featureManager");
    }

    [Fact]
    public async Task FeatureManagementFeatureFlags_IsEnabledAsync_ThrowsWhenFeatureNameWhitespace()
    {
        Mock<IFeatureManager> featureManager = new();
        IFeatureFlags sut = new FeatureManagementFeatureFlags(featureManager.Object);

        Func<Task> act = async () => await sut.IsEnabledAsync("  ");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task FeatureManagementAuthorityPipelineModeResolver_WhenSqlStorageAndFlagTrue_ReturnsTrue()
    {
        Mock<IFeatureFlags> featureFlags = new();
        featureFlags
            .Setup(f => f.IsEnabledAsync(AuthorityPipelineFeatureFlags.AsyncAuthorityPipeline, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["ArchLucid:StorageProvider"] = "Sql" })
            .Build();

        FeatureManagementAuthorityPipelineModeResolver sut = new(featureFlags.Object, configuration);

        bool queue = await sut.ShouldQueueContextAndGraphStagesAsync();

        queue.Should().BeTrue();
        featureFlags.Verify(
            f => f.IsEnabledAsync(AuthorityPipelineFeatureFlags.AsyncAuthorityPipeline, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task FeatureManagementAuthorityPipelineModeResolver_WhenSqlStorageAndFlagFalse_ReturnsFalse()
    {
        Mock<IFeatureFlags> featureFlags = new();
        featureFlags
            .Setup(f => f.IsEnabledAsync(AuthorityPipelineFeatureFlags.AsyncAuthorityPipeline, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["ArchLucid:StorageProvider"] = "Sql" })
            .Build();

        FeatureManagementAuthorityPipelineModeResolver sut = new(featureFlags.Object, configuration);

        bool queue = await sut.ShouldQueueContextAndGraphStagesAsync();

        queue.Should().BeFalse();
    }

    [Fact]
    public async Task FeatureManagementAuthorityPipelineModeResolver_WhenInMemoryStorage_DoesNotQueryFeatureFlags()
    {
        Mock<IFeatureFlags> featureFlags = new();

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["ArchLucid:StorageProvider"] = "InMemory" })
            .Build();

        FeatureManagementAuthorityPipelineModeResolver sut = new(featureFlags.Object, configuration);

        bool queue = await sut.ShouldQueueContextAndGraphStagesAsync();

        queue.Should().BeFalse();
        featureFlags.Verify(
            f => f.IsEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
