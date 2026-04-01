using ArchiForge.Data.Repositories;

namespace ArchiForge.Persistence.Tests.Contracts;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class InMemoryArchitectureRequestRepositoryContractTests : ArchitectureRequestRepositoryContractTests
{
    protected override IArchitectureRequestRepository CreateRepository()
    {
        return new InMemoryArchitectureRequestRepository();
    }
}
