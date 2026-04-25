using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Requests;

namespace ArchLucid.Decisioning.Merge;

/// <summary>
///     Merges agent results, evaluations, and decision nodes into a validated <see cref="GoldenManifest" />
///     and a set of decision traces.
/// </summary>
public interface IDecisionEngineService
{
    /// <summary>
    ///     Merges all inputs for a single architecture run into a <see cref="DecisionMergeResult" />.
    /// </summary>
    /// <param name="runId">The identifier of the run being merged. Must not be blank.</param>
    /// <param name="request">The original architecture request supplying system name, constraints, and required capabilities.</param>
    /// <param name="manifestVersion">The version string to stamp on the resulting manifest. Must not be blank.</param>
    /// <param name="results">Agent results to merge. Must contain at least one entry.</param>
    /// <param name="evaluations">Peer evaluation signals to apply as confidence adjustments.</param>
    /// <param name="decisionNodes">Decision nodes produced by <see cref="IDecisionEngineV2" /> for this run.</param>
    /// <param name="parentManifestVersion">Optional parent manifest version for incremental manifests.</param>
    /// <returns>
    ///     A <see cref="DecisionMergeResult" /> where <see cref="DecisionMergeResult.Success" /> is <see langword="true" />
    ///     and <see cref="DecisionMergeResult.Manifest" /> is populated on success, or
    ///     <see cref="DecisionMergeResult.Errors" />
    ///     is non-empty on failure (the manifest may be partially populated in that case).
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="request" />, <paramref name="evaluations" />, or <paramref name="decisionNodes" /> is
    ///     <see langword="null" />.
    /// </exception>
    DecisionMergeResult MergeResults(
        string runId,
        ArchitectureRequest request,
        string manifestVersion,
        IReadOnlyCollection<AgentResult> results,
        IReadOnlyCollection<AgentEvaluation> evaluations,
        IReadOnlyCollection<DecisionNode> decisionNodes,
        string? parentManifestVersion = null);
}
