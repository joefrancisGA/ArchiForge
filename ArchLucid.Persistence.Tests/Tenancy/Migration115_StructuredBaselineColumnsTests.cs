using System.Reflection;

using ArchLucid.Persistence.Data.Infrastructure;

namespace ArchLucid.Persistence.Tests.Tenancy;

[Trait("Category", "Unit")]
[Trait("Suite", "Migration115")]
public sealed class Migration115_StructuredBaselineColumnsTests
{
    [Fact]
    public void Embedded_115_migration_adds_tenant_baseline_intake_and_checks()
    {
        Assembly asm = typeof(DatabaseMigrator).Assembly;

        string? resourceName = asm.GetManifestResourceNames()
            .SingleOrDefault(static n => n.EndsWith("115_Tenants_StructuredBaseline.sql", StringComparison.Ordinal));

        resourceName.Should().NotBeNull();

        using Stream? stream = asm.GetManifestResourceStream(resourceName);

        stream.Should().NotBeNull();

        using StreamReader reader = new(stream!);
        string sql = reader.ReadToEnd();

        sql.Should().Contain("BaselineManualPrepHoursPerReview");
        sql.Should().Contain("CK_Tenants_BaselineManualPrepHoursPerReview_Positive");
        sql.Should().Contain("CK_Tenants_BaselinePeoplePerReview_Positive");
        sql.Should().Contain("CK_Tenants_ArchitectureTeamSize_Positive");
    }
}
