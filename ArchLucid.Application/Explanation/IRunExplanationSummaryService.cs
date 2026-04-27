using ArchLucid.Core.Explanation;
using ArchLucid.Core.Scoping;

namespace ArchLucid.Application.Explanation;

/// <summary>
///     Builds an aggregated <see cref="RunExplanationSummary" /> for dashboard and executive views.
/// </summary>
public interface IRunExplanationSummaryService
{
    /// <returns><see langword="null" /> when the run or golden manifest is missing in scope.</returns>
    Task<RunExplanationSummary?> GetSummaryAsync(ScopeContext scope, Guid runId, CancellationToken ct);
}
