using ArchLucid.Contracts.Findings;
using ArchLucid.Core.Scoping;

namespace ArchLucid.Persistence.Interfaces;

/// <summary>
///     Scoped SQL read for the operator finding inspector (single round-trip per lookup).
/// </summary>
public interface IFindingInspectReadRepository
{
    /// <summary>
    ///     Returns the inspector payload when a <see cref="Decisioning.Models.Finding" /> exists in scope; otherwise
    ///     <see langword="null" />.
    /// </summary>
    Task<FindingInspectResponse?> GetInspectAsync(ScopeContext scope, string findingId, CancellationToken ct);
}
