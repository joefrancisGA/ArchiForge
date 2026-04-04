using System.Reflection;

using ArchiForge.Persistence.Data.Infrastructure;

using FluentAssertions;

namespace ArchiForge.Persistence.Tests.Data.Infrastructure;

[Trait("Category", "Unit")]
public sealed class DatabaseMigratorTests
{
    [Fact]
    public void GetOrderedMigrationResourceNames_IsLexicographicOrdinalIgnoreCase_AndNonEmpty()
    {
        Assembly asm = typeof(DatabaseMigrator).Assembly;

        List<string> expected = asm.GetManifestResourceNames()
            .Where(static n =>
                n.Contains(".Migrations.", StringComparison.OrdinalIgnoreCase) &&
                n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(static n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        IReadOnlyList<string> actual = DatabaseMigrator.GetOrderedMigrationResourceNames();

        actual.Should().Equal(expected);
        actual.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RunExcludingTrailingScripts_Throws_WhenSkipNotPositive(int trailingScriptCountToSkip)
    {
        Action act = () => DatabaseMigrator.RunExcludingTrailingScripts("ignored", trailingScriptCountToSkip);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("trailingScriptCountToSkip");
    }

    [Fact]
    public void RunExcludingTrailingScripts_Throws_WhenSkipGreaterOrEqualToScriptCount()
    {
        int count = DatabaseMigrator.GetOrderedMigrationResourceNames().Count;

        Action actEqualToCount = () => DatabaseMigrator.RunExcludingTrailingScripts("ignored", count);
        Action actAboveCount = () => DatabaseMigrator.RunExcludingTrailingScripts("ignored", count + 1);

        actEqualToCount.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("trailingScriptCountToSkip");
        actAboveCount.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("trailingScriptCountToSkip");
    }
}
