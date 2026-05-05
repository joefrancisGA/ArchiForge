using FluentAssertions;

namespace ArchLucid.Persistence.MigrateVerify.Tests;

/// <summary>
///     Process env is mutated per-instance to avoid flaky parallel workers when multiple tests toggle
///     <see cref="Program.ConnectionStringEnvironmentVariableName" />.
/// </summary>
public sealed class TryReadConnectionStringTests : IDisposable
{
    private readonly string? _savedConnectionStringEnvironmentValue;

    public TryReadConnectionStringTests()
    {
        _savedConnectionStringEnvironmentValue =
            Environment.GetEnvironmentVariable(Program.ConnectionStringEnvironmentVariableName);

        Environment.SetEnvironmentVariable(Program.ConnectionStringEnvironmentVariableName, null);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(
            Program.ConnectionStringEnvironmentVariableName,
            _savedConnectionStringEnvironmentValue);
    }

    [Fact]
    public void When_environment_and_arguments_absent_returns_false()
    {
        bool ok =
            Program.TryReadConnectionString([], out string cs, out string err);

        ok.Should().BeFalse();
        cs.Should().BeEmpty();
        err.Should().Contain(Program.ConnectionStringEnvironmentVariableName);
    }

    [Fact]
    public void When_argument_has_initial_catalog_returns_true()
    {
        string expected =
            "Server=127.0.0.1,1433;User Id=sa;Password=test;Encrypt=True;TrustServerCertificate=True;"
            + "Initial Catalog=ArchLucidMigrateVerify";


        bool ok = Program.TryReadConnectionString([expected], out string cs, out string err);

        ok.Should().BeTrue();
        err.Should().BeEmpty();
        cs.Should().Be(expected);
    }

    [Fact]
    public void When_connection_string_missing_initial_catalog_returns_false()
    {
        string missingCatalog =
            "Server=127.0.0.1,1433;User Id=sa;Password=test;Encrypt=True;TrustServerCertificate=True";

        bool ok = Program.TryReadConnectionString([missingCatalog], out string cs, out string err);

        ok.Should().BeFalse();
        cs.Should().BeEmpty();
        err.Should().Be("Initial Catalog is required.");
    }

    [Fact]
    public void When_environment_provides_initial_catalog_even_if_arguments_empty_returns_true()
    {
        string expected =
            "Server=127.0.0.1,1433;User Id=sa;Password=test;Encrypt=True;TrustServerCertificate=True;"
            + "Initial Catalog=FromEnv";


        Environment.SetEnvironmentVariable(
            Program.ConnectionStringEnvironmentVariableName,
            expected);

        bool ok = Program.TryReadConnectionString([], out string cs, out string err);

        ok.Should().BeTrue();
        err.Should().BeEmpty();
        cs.Should().Be(expected);
    }
}
