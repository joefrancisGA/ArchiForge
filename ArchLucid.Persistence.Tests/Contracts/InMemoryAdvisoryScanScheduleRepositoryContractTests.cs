namespace ArchLucid.Persistence.Tests.Contracts;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class InMemoryAdvisoryScanScheduleRepositoryContractTests : AdvisoryScanScheduleRepositoryContractTests
{
    protected override IAdvisoryScanScheduleRepository CreateRepository()
    {
        return new InMemoryAdvisoryScanScheduleRepository();
    }
}
