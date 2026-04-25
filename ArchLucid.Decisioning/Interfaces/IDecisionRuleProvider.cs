using ArchLucid.Decisioning.Models;

namespace ArchLucid.Decisioning.Interfaces;

public interface IDecisionRuleProvider
{
    Task<DecisionRuleSet> GetRuleSetAsync(CancellationToken ct);
}
