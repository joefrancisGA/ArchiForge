using System.Reflection;

using ArchLucid.Persistence.Data.Infrastructure;

namespace ArchLucid.Persistence.Tests.Billing;

[Trait("Category", "Unit")]
public sealed class Migration086BillingSqlTests
{
    [Fact]
    public void Embedded_086_migration_contains_change_procs()
    {
        Assembly asm = typeof(DatabaseMigrator).Assembly;

        string? resourceName = asm.GetManifestResourceNames()
            .SingleOrDefault(static n =>
                n.EndsWith("086_Billing_MarketplaceChangePlanQuantity.sql", StringComparison.Ordinal));

        resourceName.Should().NotBeNull();

        using Stream? stream = asm.GetManifestResourceStream(resourceName);

        stream.Should().NotBeNull();

        using StreamReader reader = new(stream);
        string sql = reader.ReadToEnd();

        sql.Should().Contain("dbo.sp_Billing_ChangePlan");
        sql.Should().Contain("dbo.sp_Billing_ChangeQuantity");
        sql.Should().Contain("GRANT EXECUTE ON OBJECT::dbo.sp_Billing_ChangePlan");
    }
}
