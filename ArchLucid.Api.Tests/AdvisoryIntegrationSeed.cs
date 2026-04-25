using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Advisory.Scheduling;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

using Microsoft.Extensions.DependencyInjection;

using AuthorityGoldenManifestRepository = ArchLucid.Decisioning.Interfaces.IGoldenManifestRepository;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Shared in-memory authority graph for advisory HTTP integration tests (<see cref="AlertLifecycleIntegrationTests" />
///     , digest delivery lifecycle).
/// </summary>
public static class AdvisoryIntegrationSeed
{
    /// <summary>
    ///     Inserts one authority run + golden manifest for <see cref="ScopeIds" /> defaults and project slug <c>default</c>.
    /// </summary>
    /// <returns>The seeded run id (useful for Ask tests that need a run anchor).</returns>
    public static async Task<Guid> SeedDefaultScopeAuthorityRunAsync(IServiceProvider services, CancellationToken ct)
    {
        using IServiceScope scope = services.CreateScope();
        AuthorityGoldenManifestRepository goldenRepo =
            scope.ServiceProvider.GetRequiredService<AuthorityGoldenManifestRepository>();
        IRunRepository runRepo = scope.ServiceProvider.GetRequiredService<IRunRepository>();

        Guid runId = Guid.NewGuid();
        Guid manifestId = Guid.NewGuid();

        GoldenManifest manifest = new()
        {
            TenantId = ScopeIds.DefaultTenant,
            WorkspaceId = ScopeIds.DefaultWorkspace,
            ProjectId = ScopeIds.DefaultProject,
            ManifestId = manifestId,
            RunId = runId,
            ContextSnapshotId = Guid.NewGuid(),
            GraphSnapshotId = Guid.NewGuid(),
            FindingsSnapshotId = Guid.NewGuid(),
            DecisionTraceId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow,
            ManifestHash = "integration-seed",
            RuleSetId = "test-rs",
            RuleSetVersion = "1",
            RuleSetHash = "test-rh"
        };

        await goldenRepo.SaveAsync(manifest, ct);

        RunRecord run = new()
        {
            TenantId = ScopeIds.DefaultTenant,
            WorkspaceId = ScopeIds.DefaultWorkspace,
            ScopeProjectId = ScopeIds.DefaultProject,
            RunId = runId,
            ProjectId = AdvisoryScanSchedule.DefaultProjectSlug,
            CreatedUtc = DateTime.UtcNow,
            GoldenManifestId = manifestId
        };

        await runRepo.SaveAsync(run, ct);

        return runId;
    }
}
