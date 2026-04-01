using ArchiForge.Persistence.Interfaces;
using ArchiForge.Persistence.Repositories;

namespace ArchiForge.Persistence.Tests.Contracts;

/// <summary>
/// Runs <see cref="RunRepositoryContractTests"/> against <see cref="InMemoryRunRepository"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class InMemoryRunRepositoryContractTests : RunRepositoryContractTests
{
    protected override IRunRepository CreateRepository()
    {
        return new InMemoryRunRepository();
    }
}
