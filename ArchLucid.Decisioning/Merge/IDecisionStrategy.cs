using ArchLucid.Contracts.Decisions;

namespace ArchLucid.Decisioning.Merge;

/// <summary>
///     Pluggable weighted-argument scoring step for <see cref="DecisionEngineV2" />.
/// </summary>
public interface IDecisionStrategy
{
    DecisionNode Build(DecisionStrategyParameters parameters);
}
