using ArchLucid.Application.Bootstrap;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

namespace ArchLucid.Host.Core.Demo;

/// <summary>
/// Default <see cref="IDemoSeedRunResolver"/>. Tries the canonical baseline run id first, then a bounded scan over recent runs
/// filtered by <see cref="ContosoRetailDemoIdentifiers.IsDemoRequestId"/> and committed manifest presence.
/// </summary>
public sealed class DemoSeedRunResolver(IRunRepository runRepository, ILogger<DemoSeedRunResolver> logger) : IDemoSeedRunResolver
{
    /// <summary>
    /// Cap on the recent-run scan so a host with thousands of demo runs cannot turn the gated demo
    /// route into an unbounded query. 100 is well above the canonical seed surface (2 runs) and the
    /// per-tenant multi-catalog seed surface (≤ 4 runs).
    /// </summary>
    private const int RecentRunScanLimit = 100;

    private readonly IRunRepository _runRepository =
        runRepository ?? throw new ArgumentNullException(nameof(runRepository));

    private readonly ILogger<DemoSeedRunResolver> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<RunRecord?> ResolveLatestCommittedDemoRunAsync(CancellationToken cancellationToken = default)
    {
        ScopeContext scope = DemoScopes.BuildDemoScope();

        RunRecord? canonical = await _runRepository.GetByIdAsync(
            scope,
            ContosoRetailDemoIdentifiers.AuthorityRunBaselineId,
            cancellationToken);

        if (IsCommittedDemoRun(canonical))
            return canonical;

        IReadOnlyList<RunRecord> recent =
            await _runRepository.ListRecentInScopeAsync(scope, RecentRunScanLimit, cancellationToken);

        RunRecord? resolved = recent
            .Where(IsCommittedDemoRun)
            .OrderByDescending(r => r.CreatedUtc)
            .FirstOrDefault();

        if (resolved is null)
        {
            _logger.LogInformation(
                "Demo seed resolver: no committed demo-seed run found in scope {TenantId}.",
                scope.TenantId);
        }

        return resolved;
    }

    /// <summary>
    /// A "committed demo run" is a run whose <see cref="RunRecord.ArchitectureRequestId"/> matches one
    /// of the demo request shapes <em>and</em> already has a non-empty <see cref="RunRecord.GoldenManifestId"/>.
    /// </summary>
    private static bool IsCommittedDemoRun(RunRecord? run)
    {
        if (run is null)
            return false;

        if (!ContosoRetailDemoIdentifiers.IsDemoRequestId(run.ArchitectureRequestId))
            return false;

        return run.GoldenManifestId is not null && run.GoldenManifestId.Value != Guid.Empty;
    }
}
