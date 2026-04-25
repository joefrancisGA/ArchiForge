using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Persistence.Governance;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Runs <see cref="PolicyPackRepositoryContractTests" /> against <see cref="InMemoryPolicyPackRepository" />.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class InMemoryPolicyPackRepositoryContractTests : PolicyPackRepositoryContractTests
{
    protected override IPolicyPackRepository CreateRepository()
    {
        return new InMemoryPolicyPackRepository();
    }
}
