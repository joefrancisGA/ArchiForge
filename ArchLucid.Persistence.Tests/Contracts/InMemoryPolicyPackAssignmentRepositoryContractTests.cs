using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Persistence.Governance;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Runs <see cref="PolicyPackAssignmentRepositoryContractTests" /> against
///     <see cref="InMemoryPolicyPackAssignmentRepository" />.
/// </summary>
[Trait("Category", "Unit")]
public sealed class InMemoryPolicyPackAssignmentRepositoryContractTests : PolicyPackAssignmentRepositoryContractTests
{
    protected override IPolicyPackAssignmentRepository CreateRepository()
    {
        return new InMemoryPolicyPackAssignmentRepository();
    }
}
