using ArchLucid.Core.Scoping;

namespace ArchLucid.Core.Authority;

/// <summary>
///     Resolves whether the current tenant scope already completed at least one committed golden-manifest architecture
///     review — used only for UX hints (narrow default nav until first commit).
/// </summary>
public interface ICommittedArchitectureReviewFlagReader
{
    /// <summary>
    ///     Returns <see langword="true" /> when a recent run in <paramref name="scope" /> is committed with a golden
    ///     manifest id.
    /// </summary>
    Task<bool> TenantHasCommittedArchitectureReviewAsync(ScopeContext scope, CancellationToken cancellationToken);
}
