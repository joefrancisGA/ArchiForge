using System.Reflection;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Tenancy;

[Trait("Category", "Unit")]
public sealed class Migration079TenantLifecycleSqlTests
{
    [Fact]
    public void Embedded_079_migration_contains_tenant_lifecycle_transitions()
    {
        Assembly asm = typeof(ArchLucid.Persistence.Data.Infrastructure.DatabaseMigrator).Assembly;

        string? resourceName = asm.GetManifestResourceNames()
            .SingleOrDefault(static n => n.EndsWith("079_TenantLifecycleTransitions.sql", StringComparison.Ordinal));

        resourceName.Should().NotBeNull();

        using Stream? stream = asm.GetManifestResourceStream(resourceName!);

        stream.Should().NotBeNull();

        using StreamReader reader = new(stream!);
        string sql = reader.ReadToEnd();

        sql.Should().Contain("dbo.TenantLifecycleTransitions");
        sql.Should().Contain("FromStatus");
        sql.Should().Contain("ToStatus");
        sql.Should().Contain("OccurredUtc");
    }
}
