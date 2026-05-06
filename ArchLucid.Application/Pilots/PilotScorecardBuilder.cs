using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.Pilots;
/// <summary>Builds tenant-scoped pilot scorecard aggregates from <see cref = "IRunRepository"/> (read-only).</summary>
public sealed class PilotScorecardBuilder(IRunRepository runRepository, IScopeContextProvider scopeContextProvider, ILogger<PilotScorecardBuilder> logger)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(runRepository, scopeContextProvider, logger);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Persistence.Interfaces.IRunRepository runRepository, ArchLucid.Core.Scoping.IScopeContextProvider scopeContextProvider, Microsoft.Extensions.Logging.ILogger<ArchLucid.Application.Pilots.PilotScorecardBuilder> logger)
    {
        ArgumentNullException.ThrowIfNull(runRepository);
        ArgumentNullException.ThrowIfNull(scopeContextProvider);
        ArgumentNullException.ThrowIfNull(logger);
        return (byte)0;
    }

    private readonly ILogger<PilotScorecardBuilder> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IRunRepository _runRepository = runRepository ?? throw new ArgumentNullException(nameof(runRepository));
    private readonly IScopeContextProvider _scopeContextProvider = scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));
    /// <summary>
    ///     Aggregates recent runs in scope; filters to <paramref name = "periodStart"/> inclusive and
    ///     <paramref name = "periodEnd"/> exclusive (UTC).
    /// </summary>
    public async Task<PilotScorecardSummary> BuildAsync(DateTimeOffset periodStart, DateTimeOffset periodEnd, CancellationToken cancellationToken = default)
    {
        if (periodEnd <= periodStart)
            throw new ArgumentException("periodEnd must be after periodStart.", nameof(periodEnd));
        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        IReadOnlyList<RunRecord> recent = await _runRepository.ListRecentInScopeAsync(scope, 10_000, cancellationToken);
        DateTime startUtc = periodStart.UtcDateTime;
        DateTime endUtc = periodEnd.UtcDateTime;
        List<RunRecord> inWindow = recent.Where(r => r.CreatedUtc >= startUtc && r.CreatedUtc < endUtc).ToList();
        int committed = inWindow.Count(static r => !string.IsNullOrWhiteSpace(r.CurrentManifestVersion) || (r.GoldenManifestId is not null && r.GoldenManifestId.Value != Guid.Empty));
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Pilot scorecard: tenant {TenantId}, window {Start:o}–{End:o}, runs {RunCount}, committed {Committed}.", scope.TenantId, startUtc, endUtc, inWindow.Count, committed);
        return new PilotScorecardSummary
        {
            TenantId = scope.TenantId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            RunsInPeriod = inWindow.Count,
            RunsWithCommittedManifest = committed
        };
    }
}