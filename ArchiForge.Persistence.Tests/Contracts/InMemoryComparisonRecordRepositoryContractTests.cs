using ArchiForge.Persistence.Data.Repositories;

namespace ArchiForge.Persistence.Tests.Contracts;

/// <summary>
/// Runs <see cref="ComparisonRecordRepositoryContractTests"/> against <see cref="InMemoryComparisonRecordRepository"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class InMemoryComparisonRecordRepositoryContractTests : ComparisonRecordRepositoryContractTests
{
    protected override IComparisonRecordRepository CreateRepository()
    {
        return new InMemoryComparisonRecordRepository();
    }
}
