using System.Reflection;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Tenancy;

[Trait("Category", "Unit")]
public sealed class Migration081TrialFunnelSqlTests
{
    [Fact]
    public void Embedded_081_migration_adds_trial_first_manifest_column()
    {
        Assembly asm = typeof(ArchLucid.Persistence.Data.Infrastructure.DatabaseMigrator).Assembly;

        string? resourceName = asm.GetManifestResourceNames()
            .SingleOrDefault(static n => n.EndsWith("081_Tenants_TrialFirstManifestCommittedUtc.sql", StringComparison.Ordinal));

        resourceName.Should().NotBeNull();

        using Stream? stream = asm.GetManifestResourceStream(resourceName);

        stream.Should().NotBeNull();

        using StreamReader reader = new(stream);
        string sql = reader.ReadToEnd();

        sql.Should().Contain("TrialFirstManifestCommittedUtc");
    }
}
