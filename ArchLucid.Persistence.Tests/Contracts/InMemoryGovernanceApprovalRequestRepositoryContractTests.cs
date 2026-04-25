using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Persistence.Tests.Contracts;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class
    InMemoryGovernanceApprovalRequestRepositoryContractTests : GovernanceApprovalRequestRepositoryContractTests
{
    protected override IGovernanceApprovalRequestRepository CreateRepository()
    {
        return new InMemoryGovernanceApprovalRequestRepository();
    }
}
