using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Repositories;

namespace ArchiForge.Persistence.Tests.Contracts;

/// <summary>
/// Runs <see cref="GoldenManifestRepositoryContractTests"/> against <see cref="InMemoryGoldenManifestRepository"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class InMemoryGoldenManifestRepositoryContractTests : GoldenManifestRepositoryContractTests
{
    protected override IGoldenManifestRepository CreateRepository()
    {
        return new InMemoryGoldenManifestRepository();
    }
}
