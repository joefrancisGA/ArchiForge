using ArchLucid.Contracts.Decisions;

namespace ArchLucid.Decisioning.Merge;

/// <summary>
///     Pluggable weighted-argument scoring step for <see cref="DecisionEngineV2" />.
/// </summary>
public interface IDecisionStrategy
{
    /// <summary>
    ///     Builds a single <see cref="DecisionNode" /> from shared run context and agent evaluations.
    /// </summary>
    /// <param name="parameters">
    ///     Correlation id, clock, evaluations, and optional task/topology slices required by the concrete strategy.
    ///     Strategies validate non-null expectations (e.g. <see cref="DecisionStrategyParameters.TopologyTask" /> for topology acceptance).
    /// </param>
    /// <returns>
    ///     A fully populated <see cref="DecisionNode" /> including options, selected option id, confidence (<see cref="DecisionOption.FinalScore" />),
    ///     rationale, and evaluation id groupings for audit.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">
    ///     Thrown when <paramref name="parameters" /> or a required nested input is <see langword="null" />.
    /// </exception>
    DecisionNode Build(DecisionStrategyParameters parameters);
}
