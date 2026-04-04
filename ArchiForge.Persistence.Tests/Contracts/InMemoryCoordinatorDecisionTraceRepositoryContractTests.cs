using ArchiForge.Persistence.Data.Repositories;

namespace ArchiForge.Persistence.Tests.Contracts;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class InMemoryCoordinatorDecisionTraceRepositoryContractTests : CoordinatorDecisionTraceRepositoryContractTests
{
    protected override IDecisionTraceRepository CreateRepository()
    {
        return new InMemoryCoordinatorDecisionTraceRepository();
    }
}
