using ArchiForge.Persistence.Data.Repositories;

namespace ArchiForge.Persistence.Tests.Contracts;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class InMemoryArchitectureRunIdempotencyRepositoryContractTests
    : ArchitectureRunIdempotencyRepositoryContractTests
{
    protected override IArchitectureRunIdempotencyRepository CreateRepository()
    {
        return new InMemoryArchitectureRunIdempotencyRepository();
    }
}
