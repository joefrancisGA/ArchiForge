using ArchiForge.Persistence.Data.Repositories;

namespace ArchiForge.Persistence.Tests.Contracts;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class InMemoryGovernanceEnvironmentActivationRepositoryContractTests : GovernanceEnvironmentActivationRepositoryContractTests
{
    protected override IGovernanceEnvironmentActivationRepository CreateRepository()
    {
        return new InMemoryGovernanceEnvironmentActivationRepository();
    }
}
