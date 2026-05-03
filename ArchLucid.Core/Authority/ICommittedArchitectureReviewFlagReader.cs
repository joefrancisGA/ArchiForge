using ArchLucid.Core.Scoping;

namespace ArchLucid.Core.Authority;

/// <summary>
///     Resolves whether the current tenant scope already completed at least one committed golden-manifest architecture
///     review — used only for UX hints (narrow default nav until first commit).
/// </summary>
public interface ICommittedArchitectureReviewFlagReader
{
    /// <summary>
    ///     Returns <see langword="true" /> when <paramref name="scope" /> has at least one committed architecture review
    ///     backed by a persisted golden manifest row — UX-only signal for narrowing default operator nav until first commit.
    /// </summary>
    Task<bool> TenantHasCommittedArchitectureReviewAsync(ScopeContext scope, CancellationToken cancellationToken);
}
