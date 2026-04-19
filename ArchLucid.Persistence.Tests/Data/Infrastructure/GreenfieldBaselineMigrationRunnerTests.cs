using ArchLucid.Persistence.Data.Infrastructure;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Data.Infrastructure;

[Trait("Category", "Unit")]
public sealed class GreenfieldBaselineMigrationRunnerTests
{
    [Fact]
    public void GetOrderedIncrementalMigrationResourceNames_Places017_GovernanceWorkflow_before017_GraphSnapshots()
    {
        IReadOnlyList<string> names = GreenfieldBaselineMigrationRunner.GetOrderedIncrementalMigrationResourceNames();

        int governance = names
            .Select((string n, int i) => (n, i))
            .First(t => t.n.Contains("017_GovernanceWorkflow", StringComparison.OrdinalIgnoreCase))
            .i;
        int graphSnapshots = names
            .Select((string n, int i) => (n, i))
            .First(t => t.n.Contains("017_GraphSnapshots", StringComparison.OrdinalIgnoreCase))
            .i;

        governance.Should().BeLessThan(graphSnapshots);
    }

    [Theory]
    [InlineData("There is already an object named 'ArchitectureRequests' in the database.", 0, true)]
    [InlineData("There is already an object named 'GovernanceApprovalRequests' in the database.", 0, true)]
    [InlineData("Msg 2714, Level 16, State 6, Line 1: There is already an object named 'GovernancePromotionRecords' in the database.", 2714, true)]
    [InlineData("There is already an object named 'GovernanceEnvironmentActivations' in the database.", 2714, true)]
    [InlineData("There is already an object named 'OtherTable' in the database.", 2714, false)]
    [InlineData("select * from ArchitectureRequests", 0, false)]
    [InlineData("", 2714, false)]
    public void IsKnownDuplicateInitialMigrationTable_matches_architecture_and_governance_duplicate_messages(
        string message,
        int errorNumber,
        bool expected)
    {
        bool actual = GreenfieldBaselineMigrationRunner.IsKnownDuplicateInitialMigrationTable(message, errorNumber);

        actual.Should().Be(expected);
    }
}
