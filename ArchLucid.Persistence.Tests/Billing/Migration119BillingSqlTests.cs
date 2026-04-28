using System.Reflection;

using ArchLucid.Persistence.Data.Infrastructure;

namespace ArchLucid.Persistence.Tests.Billing;

[Trait("Category", "Unit")]
public sealed class Migration119BillingSqlTests
{
    [Fact]
    public void Embedded_119_migration_contains_history_table_append_proc_and_billing_procs()
    {
        Assembly asm = typeof(DatabaseMigrator).Assembly;

        string? resourceName = asm.GetManifestResourceNames()
            .SingleOrDefault(static n => n.EndsWith("119_BillingSubscriptionStateHistory.sql", StringComparison.Ordinal));

        resourceName.Should().NotBeNull();

        using Stream? stream = asm.GetManifestResourceStream(resourceName);

        stream.Should().NotBeNull();

        using StreamReader reader = new(stream);
        string sql = reader.ReadToEnd();

        sql.Should().Contain("dbo.BillingSubscriptionStateHistory");
        sql.Should().Contain("sp_Billing_AppendStateHistory");
        sql.Should().Contain("sp_Billing_UpsertPending");
        sql.Should().Contain("DENY INSERT ON dbo.BillingSubscriptionStateHistory");
        sql.Should().Contain("ADD FILTER PREDICATE");
        sql.Should().Contain("BillingSubscriptionStateHistory");
    }
}
