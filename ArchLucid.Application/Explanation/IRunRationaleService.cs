using ArchLucid.Core.Explanation;
using ArchLucid.Core.Scoping;

namespace ArchLucid.Application.Explanation;

/// <summary>
///     Builds a unified <see cref="RunRationale" /> from authority run detail and optional architecture run
///     aggregate.
/// </summary>
public interface IRunRationaleService
{
    /// <summary>
    ///     Returns rationale for <paramref name="runId" /> in <paramref name="scope" />, or <see langword="null" /> when the
    ///     authority run row is missing.
    /// </summary>
    Task<RunRationale?> GetRunRationaleAsync(ScopeContext scope, Guid runId, CancellationToken ct);
}
