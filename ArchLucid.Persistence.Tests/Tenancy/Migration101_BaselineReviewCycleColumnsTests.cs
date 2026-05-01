using System.Reflection;

using ArchLucid.Persistence.Data.Infrastructure;

namespace ArchLucid.Persistence.Tests.Tenancy;

[Trait("Category", "Unit")]
public sealed class Migration101_BaselineReviewCycleColumnsTests
{
    [SkippableFact]
    public void Embedded_101_migration_adds_baseline_columns_and_positive_check()
    {
        Assembly asm = typeof(DatabaseMigrator).Assembly;

        string? resourceName = asm.GetManifestResourceNames()
            .SingleOrDefault(static n => n.EndsWith("101_Tenants_BaselineReviewCycle.sql", StringComparison.Ordinal));

        resourceName.Should().NotBeNull();

        using Stream? stream = asm.GetManifestResourceStream(resourceName);

        stream.Should().NotBeNull();

        using StreamReader reader = new(stream);
        string sql = reader.ReadToEnd();

        sql.Should().Contain("BaselineReviewCycleHours");
        sql.Should().Contain("BaselineReviewCycleSource");
        sql.Should().Contain("BaselineReviewCycleCapturedUtc");
        sql.Should().Contain("CK_Tenants_BaselineReviewCycleHours_Positive");
    }
}
