using ArchLucid.Core.Scoping;

namespace ArchLucid.Persistence.Scoping;

/// <summary>
///     Returns an empty scope triple for CLI / tools that persist SQL rows without HTTP ambient scope.
///     Prefer a real <see cref="IScopeContextProvider" /> in hosted scenarios; RLS bypass is typical for backfill jobs.
/// </summary>
public sealed class EmptyPersistenceScopeContextProvider : IScopeContextProvider
{
    /// <inheritdoc />
    public ScopeContext GetCurrentScope()
    {
        return new ScopeContext { TenantId = Guid.Empty, WorkspaceId = Guid.Empty, ProjectId = Guid.Empty };
    }
}
