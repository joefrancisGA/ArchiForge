using ArchiForge.Decisioning.Alerts;
using ArchiForge.Persistence.Alerts;

namespace ArchiForge.Persistence.Tests.Contracts;

/// <summary>
/// Runs <see cref="AlertRuleRepositoryContractTests"/> against <see cref="InMemoryAlertRuleRepository"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class InMemoryAlertRuleRepositoryContractTests : AlertRuleRepositoryContractTests
{
    protected override IAlertRuleRepository CreateRepository()
    {
        return new InMemoryAlertRuleRepository();
    }
}
