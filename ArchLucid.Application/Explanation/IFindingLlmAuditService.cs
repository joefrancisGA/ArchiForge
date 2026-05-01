using ArchLucid.Contracts.Explanation;

namespace ArchLucid.Application.Explanation;

/// <summary>
///     Resolves the best-matching <see cref="Contracts.Agents.AgentExecutionTrace" /> for a finding and returns deny-list
///     redacted text for operator review.
/// </summary>
public interface IFindingLlmAuditService
{
    /// <summary>
    ///     Returns redacted LLM audit text, or <see langword="null" /> when the run/finding is missing or no trace can be
    ///     resolved.
    /// </summary>
    Task<FindingLlmAuditResult?> BuildAsync(
        Guid runId,
        string findingId,
        CancellationToken cancellationToken = default);
}
