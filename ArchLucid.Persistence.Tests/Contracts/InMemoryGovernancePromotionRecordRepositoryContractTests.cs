using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Persistence.Tests.Contracts;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class
    InMemoryGovernancePromotionRecordRepositoryContractTests : GovernancePromotionRecordRepositoryContractTests
{
    protected override IGovernancePromotionRecordRepository CreateRepository()
    {
        return new InMemoryGovernancePromotionRecordRepository();
    }
}
