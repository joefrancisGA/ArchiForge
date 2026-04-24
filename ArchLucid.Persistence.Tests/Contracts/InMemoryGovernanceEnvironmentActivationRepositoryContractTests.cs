using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Persistence.Tests.Contracts;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class
    InMemoryGovernanceEnvironmentActivationRepositoryContractTests :
    GovernanceEnvironmentActivationRepositoryContractTests
{
    protected override IGovernanceEnvironmentActivationRepository CreateRepository()
    {
        return new InMemoryGovernanceEnvironmentActivationRepository();
    }
}
