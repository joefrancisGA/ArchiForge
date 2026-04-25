using ArchLucid.Persistence.Audit;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Runs <see cref="AuditRepositoryContractTests" /> against <see cref="InMemoryAuditRepository" />.
/// </summary>
[Trait("Category", "Unit")]
public sealed class InMemoryAuditRepositoryContractTests : AuditRepositoryContractTests
{
    protected override IAuditRepository CreateRepository()
    {
        return new InMemoryAuditRepository();
    }
}
