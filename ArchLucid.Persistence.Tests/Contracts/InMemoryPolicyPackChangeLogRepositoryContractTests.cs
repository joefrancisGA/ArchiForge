using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Persistence.Governance;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Runs <see cref="PolicyPackChangeLogRepositoryContractTests" /> against
///     <see cref="InMemoryPolicyPackChangeLogRepository" />.
/// </summary>
[Trait("Category", "Unit")]
public sealed class InMemoryPolicyPackChangeLogRepositoryContractTests : PolicyPackChangeLogRepositoryContractTests
{
    protected override IPolicyPackChangeLogRepository CreateRepository()
    {
        return new InMemoryPolicyPackChangeLogRepository();
    }
}
