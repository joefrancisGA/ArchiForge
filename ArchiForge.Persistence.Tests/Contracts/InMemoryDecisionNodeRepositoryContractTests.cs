using ArchiForge.Persistence.Data.Repositories;

namespace ArchiForge.Persistence.Tests.Contracts;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class InMemoryDecisionNodeRepositoryContractTests : DecisionNodeRepositoryContractTests
{
    protected override IDecisionNodeRepository CreateRepository()
    {
        return new InMemoryDecisionNodeRepository();
    }
}
