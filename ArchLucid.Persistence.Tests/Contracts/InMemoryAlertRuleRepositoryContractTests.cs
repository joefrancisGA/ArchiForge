using ArchLucid.Decisioning.Alerts;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Runs <see cref="AlertRuleRepositoryContractTests" /> against <see cref="InMemoryAlertRuleRepository" />.
/// </summary>
[Trait("Category", "Unit")]
public sealed class InMemoryAlertRuleRepositoryContractTests : AlertRuleRepositoryContractTests
{
    protected override IAlertRuleRepository CreateRepository()
    {
        return new InMemoryAlertRuleRepository();
    }
}
