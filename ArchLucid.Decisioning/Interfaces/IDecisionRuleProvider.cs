using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Interfaces;

public interface IDecisionRuleProvider
{
    Task<DecisionRuleSet> GetRuleSetAsync(CancellationToken ct);
}

