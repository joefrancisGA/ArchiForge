using System.Reflection;

using ArchLucid.Persistence.Data.Infrastructure;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Governance;

[Trait("Category", "Unit")]
public sealed class Migration080PolicyPackVersionsSqlTests
{
    [Fact]
    public void Embedded_080_migration_enforces_unique_pack_version()
    {
        Assembly asm = typeof(DatabaseMigrator).Assembly;

        string? resourceName = asm.GetManifestResourceNames()
            .SingleOrDefault(static n =>
                n.EndsWith("080_PolicyPackVersions_UniquePackVersion.sql", StringComparison.Ordinal));

        resourceName.Should().NotBeNull();

        using Stream? stream = asm.GetManifestResourceStream(resourceName);

        stream.Should().NotBeNull();

        using StreamReader reader = new(stream);
        string sql = reader.ReadToEnd();

        sql.Should().Contain("UQ_PolicyPackVersions_PolicyPackId_Version");
        sql.Should().Contain("UNIQUE NONCLUSTERED");
    }
}
