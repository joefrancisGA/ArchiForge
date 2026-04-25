using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Repositories;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Runs <see cref="DecisionTraceRepositoryContractTests" /> against <see cref="InMemoryDecisionTraceRepository" />.
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
