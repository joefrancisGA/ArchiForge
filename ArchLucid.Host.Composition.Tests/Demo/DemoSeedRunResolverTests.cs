using ArchLucid.Application.Bootstrap;
using ArchLucid.Core.Scoping;
using ArchLucid.Host.Core.Demo;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Host.Composition.Tests.Demo;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class DemoSeedRunResolverTests
{
    [Fact]
    public async Task ResolveLatestCommittedDemoRunAsync_returns_canonical_baseline_when_committed()
    {
        Guid manifestId = Guid.NewGuid();
        RunRecord baseline = new()
        {
            RunId = ContosoRetailDemoIdentifiers.AuthorityRunBaselineId,
            ArchitectureRequestId = ContosoRetailDemoIdentifiers.RequestContoso,
            GoldenManifestId = manifestId,
            CreatedUtc = DateTime.UtcNow
        };

        Mock<IRunRepository> runRepo = new();
        runRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<ScopeContext>(), ContosoRetailDemoIdentifiers.AuthorityRunBaselineId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(baseline);

        DemoSeedRunResolver sut = new(runRepo.Object, NullLogger<DemoSeedRunResolver>.Instance);

        RunRecord? resolved = await sut.ResolveLatestCommittedDemoRunAsync();

        resolved.Should().NotBeNull();
        resolved.RunId.Should().Be(baseline.RunId);

        runRepo.Verify(
            r => r.ListRecentInScopeAsync(It.IsAny<ScopeContext>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveLatestCommittedDemoRunAsync_falls_back_to_newest_committed_demo_run()
    {
        Mock<IRunRepository> runRepo = new();
        runRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<ScopeContext>(), ContosoRetailDemoIdentifiers.AuthorityRunBaselineId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunRecord?)null);

        RunRecord newest = new()
        {
            RunId = Guid.NewGuid(),
            ArchitectureRequestId = ContosoRetailDemoIdentifiers.MultiTenantRequestPrefix + "abc",
            GoldenManifestId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow.AddMinutes(-5)
        };
        RunRecord older = new()
        {
            RunId = Guid.NewGuid(),
            ArchitectureRequestId = ContosoRetailDemoIdentifiers.RequestContoso,
            GoldenManifestId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow.AddHours(-1)
        };
        RunRecord nonDemo = new()
        {
            RunId = Guid.NewGuid(),
            ArchitectureRequestId = "request-not-demo",
            GoldenManifestId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow
        };
        RunRecord demoUncommitted = new()
        {
            RunId = Guid.NewGuid(),
            ArchitectureRequestId = ContosoRetailDemoIdentifiers.RequestContoso,
            GoldenManifestId = null,
            CreatedUtc = DateTime.UtcNow.AddMinutes(-1)
        };

        runRepo
            .Setup(r => r.ListRecentInScopeAsync(It.IsAny<ScopeContext>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RunRecord> { older, nonDemo, demoUncommitted, newest });

        DemoSeedRunResolver sut = new(runRepo.Object, NullLogger<DemoSeedRunResolver>.Instance);

        RunRecord? resolved = await sut.ResolveLatestCommittedDemoRunAsync();

        resolved.Should().NotBeNull();
        resolved.RunId.Should().Be(newest.RunId);
    }

    [Fact]
    public async Task ResolveLatestCommittedDemoRunAsync_returns_null_when_no_committed_demo_run_exists()
    {
        Mock<IRunRepository> runRepo = new();
        runRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<ScopeContext>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunRecord?)null);
        runRepo
            .Setup(r => r.ListRecentInScopeAsync(It.IsAny<ScopeContext>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        DemoSeedRunResolver sut = new(runRepo.Object, NullLogger<DemoSeedRunResolver>.Instance);

        RunRecord? resolved = await sut.ResolveLatestCommittedDemoRunAsync();

        resolved.Should().BeNull();
    }
}
