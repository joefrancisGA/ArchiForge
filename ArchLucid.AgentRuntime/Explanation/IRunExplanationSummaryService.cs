using ArchLucid.Core.Explanation;
using ArchLucid.Core.Scoping;

namespace ArchLucid.AgentRuntime.Explanation;

/// <summary>
/// Builds an aggregated <see cref="RunExplanationSummary"/> for dashboard and executive views.
/// </summary>
public interface IRunExplanationSummaryService
{
    /// <summary>
    /// Loads the run, produces the standard explanation, and derives themes and posture.
    /// </summary>
    /// <returns><see langword="null"/> when the run or golden manifest is missing in scope.</returns>
    Task<RunExplanationSummary?> GetSummaryAsync(ScopeContext scope, Guid runId, CancellationToken ct);
}
