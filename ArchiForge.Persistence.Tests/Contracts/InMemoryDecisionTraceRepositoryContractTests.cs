using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Repositories;

namespace ArchiForge.Persistence.Tests.Contracts;

/// <summary>
/// Runs <see cref="DecisionTraceRepositoryContractTests"/> against <see cref="InMemoryDecisionTraceRepository"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class InMemoryDecisionTraceRepositoryContractTests : DecisionTraceRepositoryContractTests
{
    protected override IDecisionTraceRepository CreateRepository()
    {
        return new InMemoryDecisionTraceRepository();
    }
}
