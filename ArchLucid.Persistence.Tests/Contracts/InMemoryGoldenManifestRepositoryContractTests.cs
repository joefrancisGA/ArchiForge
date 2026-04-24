using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Repositories;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Runs <see cref="GoldenManifestRepositoryContractTests" /> against <see cref="InMemoryGoldenManifestRepository" />.
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
