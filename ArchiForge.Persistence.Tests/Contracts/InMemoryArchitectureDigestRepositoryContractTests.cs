using ArchiForge.Persistence.Advisory;

namespace ArchiForge.Persistence.Tests.Contracts;

/// <summary>
/// Runs <see cref="ArchitectureDigestRepositoryContractTests"/> against <see cref="InMemoryArchitectureDigestRepository"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class InMemoryArchitectureDigestRepositoryContractTests : ArchitectureDigestRepositoryContractTests
{
    protected override IArchitectureDigestRepository CreateRepository()
    {
        return new InMemoryArchitectureDigestRepository();
    }
}
