using ArchLucid.Core.Configuration;

using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Runs.Orchestration;

/// <summary>
/// ADR 0030 PR A2 — selects coordinator vs authority <see cref="IArchitectureRunCommitOrchestrator"/> implementation
/// from <see cref="LegacyRunCommitPathOptions.LegacyRunCommitPath"/>.
/// </summary>
public sealed class RunCommitPathSelector(
    IOptionsMonitor<LegacyRunCommitPathOptions> optionsMonitor,
    ArchitectureRunCommitOrchestrator legacyCoordinatorCommit,
    AuthorityDrivenArchitectureRunCommitOrchestrator authorityCommit) : IArchitectureRunCommitOrchestrator
{
    private readonly IOptionsMonitor<LegacyRunCommitPathOptions> _optionsMonitor =
        optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));

    private readonly ArchitectureRunCommitOrchestrator _legacyCoordinatorCommit =
        legacyCoordinatorCommit ?? throw new ArgumentNullException(nameof(legacyCoordinatorCommit));

    private readonly AuthorityDrivenArchitectureRunCommitOrchestrator _authorityCommit =
        authorityCommit ?? throw new ArgumentNullException(nameof(authorityCommit));

    /// <inheritdoc />
    public Task<CommitRunResult> CommitRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        if (_optionsMonitor.CurrentValue.LegacyRunCommitPath)
            return _legacyCoordinatorCommit.CommitRunAsync(runId, cancellationToken);

        return _authorityCommit.CommitRunAsync(runId, cancellationToken);
    }
}
