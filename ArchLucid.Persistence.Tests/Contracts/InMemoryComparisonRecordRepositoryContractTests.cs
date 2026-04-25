using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Runs <see cref="ComparisonRecordRepositoryContractTests" /> against
///     <see cref="InMemoryComparisonRecordRepository" />.
/// </summary>
[Trait("Category", "Unit")]
public sealed class InMemoryComparisonRecordRepositoryContractTests : ComparisonRecordRepositoryContractTests
{
    protected override IComparisonRecordRepository CreateRepository()
    {
        return new InMemoryComparisonRecordRepository();
    }
}
