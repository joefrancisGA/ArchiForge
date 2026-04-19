using System.Reflection;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Billing;

[Trait("Category", "Unit")]
public sealed class Migration078BillingSqlTests
{
    [Fact]
    public void Embedded_078_migration_contains_rls_procs_and_app_deny()
    {
        Assembly asm = typeof(ArchLucid.Persistence.Data.Infrastructure.DatabaseMigrator).Assembly;

        string? resourceName = asm.GetManifestResourceNames()
            .SingleOrDefault(static n => n.EndsWith("078_BillingSubscriptions.sql", StringComparison.Ordinal));

        resourceName.Should().NotBeNull();

        using Stream? stream = asm.GetManifestResourceStream(resourceName);

        stream.Should().NotBeNull();

        using StreamReader reader = new(stream);
        string sql = reader.ReadToEnd();

        sql.Should().Contain("dbo.BillingSubscriptions");
        sql.Should().Contain("dbo.BillingWebhookEvents");
        sql.Should().Contain("sp_Billing_Activate");
        sql.Should().Contain("DENY INSERT ON dbo.BillingSubscriptions");
        sql.Should().Contain("ADD FILTER PREDICATE");
        sql.Should().Contain("scope_predicate(TenantId, WorkspaceId, ProjectId)");
    }
}
