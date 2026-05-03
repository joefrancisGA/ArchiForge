using ArchLucid.Contracts.Common;
using ArchLucid.Core.Authority;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

namespace ArchLucid.Persistence.Repositories;

/// <summary>
///     Uses <see cref="IRunRepository.ListRecentInScopeAsync" /> (bounded take) to detect a committed run with
///     <see cref="RunRecord.GoldenManifestId" /> without a dedicated SQL shape — acceptable for an infrequent
///     <c>/me</c> enrichment.
/// </summary>
public sealed class RunRepositoryCommittedArchitectureReviewFlagReader(IRunRepository runRepository)
    : ICommittedArchitectureReviewFlagReader
{
    private readonly IRunRepository _runRepository =
        runRepository ?? throw new ArgumentNullException(nameof(runRepository));

    private static readonly string CommittedLegacyStatus = ArchitectureRunStatus.Committed.ToString();

    /// <inheritdoc />
    public async Task<bool> TenantHasCommittedArchitectureReviewAsync(
        ScopeContext scope,
        CancellationToken cancellationToken)
    {
        if (scope is null)
            throw new ArgumentNullException(nameof(scope));

        // Dashboard-style cap; aligns with picker lists. Dedicated EXISTS remains an option if profiling says so.
        IReadOnlyList<RunRecord> recent =
            await _runRepository.ListRecentInScopeAsync(scope, take: 500, cancellationToken);

        return recent.Any(r =>
            string.Equals(r.LegacyRunStatus, CommittedLegacyStatus, StringComparison.Ordinal)
            && r.GoldenManifestId is not null);
    }
}
