using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Repositories;
using ArchLucid.Persistence.Tenancy;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Runs <see cref="RunRepositoryContractTests" /> against <see cref="InMemoryRunRepository" />.
/// </summary>
[Trait("Category", "Unit")]
public sealed class InMemoryRunRepositoryContractTests : RunRepositoryContractTests
{
    protected override IRunRepository CreateRepository()
    {
        return new InMemoryRunRepository(new InMemoryTenantRepository());
    }
}
