using ArchiForge.Persistence.Data.Repositories;

namespace ArchiForge.Persistence.Tests.Contracts;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class InMemoryGovernancePromotionRecordRepositoryContractTests : GovernancePromotionRecordRepositoryContractTests
{
    protected override IGovernancePromotionRecordRepository CreateRepository()
    {
        return new InMemoryGovernancePromotionRecordRepository();
    }
}
