using ArchiForge.Persistence.Data.Repositories;

namespace ArchiForge.Persistence.Tests.Contracts;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class InMemoryGovernanceApprovalRequestRepositoryContractTests : GovernanceApprovalRequestRepositoryContractTests
{
    protected override IGovernanceApprovalRequestRepository CreateRepository()
    {
        return new InMemoryGovernanceApprovalRequestRepository();
    }
}
