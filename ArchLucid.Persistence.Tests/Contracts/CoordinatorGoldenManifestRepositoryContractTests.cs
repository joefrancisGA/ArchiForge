using ArchLucid.Contracts.Manifest;
using ArchLucid.Persistence.Data.Repositories;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
/// Shared contract assertions for coordinator <see cref="ICoordinatorGoldenManifestRepository"/> (architecture manifest versions).
/// </summary>
public abstract class CoordinatorGoldenManifestRepositoryContractTests
{
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    protected abstract ICoordinatorGoldenManifestRepository CreateRepository();

    /// <summary>SQL: ensures <c>dbo.Runs</c> + <c>dbo.ArchitectureRequests</c> exist for coordinator manifest insert.</summary>
    protected virtual Task PrepareRunForCoordinatorDataAsync(string requestId, string runId, CancellationToken ct)
    {
        _ = requestId;
        _ = runId;
        _ = ct;

        return Task.CompletedTask;
    }

    private static GoldenManifest NewManifest(string runId, string version)
    {
        return new GoldenManifest
        {
            RunId = runId,
            SystemName = "coord-sys",
            Services = [],
            Datastores = [],
            Relationships = [],
            Governance = new ManifestGovernance(),
            Metadata = new ManifestMetadata
            {
                ManifestVersion = version,
                CreatedUtc = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc),
            },
        };
    }

    [SkippableFact]
    public async Task Create_then_GetByVersion_round_trips()
    {
        SkipIfSqlServerUnavailable();
        ICoordinatorGoldenManifestRepository repo = CreateRepository();
        string runId = Guid.NewGuid().ToString("N");
        string requestId = "req-" + Guid.NewGuid().ToString("N");
        await PrepareRunForCoordinatorDataAsync(requestId, runId, CancellationToken.None);
        GoldenManifest manifest = NewManifest(runId, "v-coord-1");

        await repo.CreateAsync(manifest, CancellationToken.None);

        GoldenManifest? loaded = await repo.GetByVersionAsync("v-coord-1", CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.RunId.Should().Be(manifest.RunId);
        loaded.Metadata.ManifestVersion.Should().Be("v-coord-1");
        loaded.SystemName.Should().Be("coord-sys");
    }

    [SkippableFact]
    public async Task GetByVersion_missing_returns_null()
    {
        SkipIfSqlServerUnavailable();
        ICoordinatorGoldenManifestRepository repo = CreateRepository();

        GoldenManifest? loaded = await repo.GetByVersionAsync("missing-" + Guid.NewGuid().ToString("N"), CancellationToken.None);

        loaded.Should().BeNull();
    }
}
