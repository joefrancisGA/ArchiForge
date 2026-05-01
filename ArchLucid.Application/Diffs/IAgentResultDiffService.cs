using ArchLucid.Contracts.Agents;

namespace ArchLucid.Application.Diffs;

/// <summary>
///     Computes a per-agent diff between the <see cref="AgentResult" /> collections of two runs,
///     identifying added, removed, and changed agent outputs.
/// </summary>
public interface IAgentResultDiffService
{
    /// <summary>
    ///     Compares the agent results from two runs and returns an <see cref="AgentResultDiffResult" />
    ///     describing per-agent additions, removals, and changes.
    /// </summary>
    /// <param name="leftRunId">Identifier of the baseline run.</param>
    /// <param name="leftResults">Agent results from the baseline run.</param>
    /// <param name="rightRunId">Identifier of the run being compared.</param>
    /// <param name="rightResults">Agent results from the run being compared.</param>
    /// <returns>A diff result; never <see langword="null" />.</returns>
    AgentResultDiffResult Compare(
        string leftRunId,
        IReadOnlyCollection<AgentResult> leftResults,
        string rightRunId,
        IReadOnlyCollection<AgentResult> rightResults);
}
