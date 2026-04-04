using ArchiForge.Persistence.Data.Repositories;

namespace ArchiForge.Persistence.Tests.Contracts;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class InMemoryAgentResultRepositoryContractTests : AgentResultRepositoryContractTests
{
    protected override IAgentResultRepository CreateRepository()
    {
        return new InMemoryAgentResultRepository();
    }
}
