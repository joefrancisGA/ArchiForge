using ArchiForge.Persistence.Provenance;

namespace ArchiForge.Persistence.Tests.Contracts;

/// <summary>
/// Runs <see cref="ProvenanceSnapshotRepositoryContractTests"/> against <see cref="InMemoryProvenanceSnapshotRepository"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class InMemoryProvenanceSnapshotRepositoryContractTests : ProvenanceSnapshotRepositoryContractTests
{
    protected override IProvenanceSnapshotRepository CreateRepository()
    {
        return new InMemoryProvenanceSnapshotRepository();
    }
}
