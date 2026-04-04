using ArchiForge.Persistence.Data.Repositories;

namespace ArchiForge.Persistence.Tests.Contracts;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class InMemoryAgentTaskRepositoryContractTests : AgentTaskRepositoryContractTests
{
    protected override IAgentTaskRepository CreateRepository()
    {
        return new InMemoryAgentTaskRepository();
    }
}
