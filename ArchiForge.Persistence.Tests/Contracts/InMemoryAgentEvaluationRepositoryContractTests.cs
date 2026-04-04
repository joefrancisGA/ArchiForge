using ArchiForge.Persistence.Data.Repositories;

namespace ArchiForge.Persistence.Tests.Contracts;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class InMemoryAgentEvaluationRepositoryContractTests : AgentEvaluationRepositoryContractTests
{
    protected override IAgentEvaluationRepository CreateRepository()
    {
        return new InMemoryAgentEvaluationRepository();
    }
}
