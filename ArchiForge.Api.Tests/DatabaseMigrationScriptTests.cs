using System.Reflection;

using FluentAssertions;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Unit")]
public sealed class DatabaseMigrationScriptTests
{
    [Fact]
    public void ArchiForgeDataAssembly_HasEmbeddedMigrationScripts_InNumericOrder()
    {
        Assembly asm = typeof(Data.Infrastructure.DatabaseMigrator).Assembly;

        List<string> scripts = asm.GetManifestResourceNames()
            .Where(n => n.Contains("Migrations", StringComparison.OrdinalIgnoreCase) &&
                        n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        scripts.Should().NotBeEmpty("DbUp expects embedded .sql migration scripts under a Migrations folder");

        // Ensure scripts are prefixed like 001_, 002_, ... to keep deterministic ordering.
        scripts.All(n =>
        {
            string file = n.Split('.').LastOrDefault() ?? n;
            return file.Length >= 4 && char.IsDigit(file[0]) && char.IsDigit(file[1]) && char.IsDigit(file[2]) && file[3] == '_';
        }).Should().BeTrue("migration scripts should start with a numeric prefix like 001_ for deterministic DbUp ordering");
    }
}

