using ArchLucid.Persistence.Connections;

using FluentAssertions;

using Microsoft.Extensions.Configuration;

namespace ArchLucid.Persistence.Tests.Connections;

[Trait("Category", "Unit")]
public sealed class RlsBreakGlassTests
{
    private const string EnvName = "ARCHLUCID_ALLOW_RLS_BYPASS";

    private static readonly object s_envGate = new();

    [Fact]
    public void IsEnabled_ReturnsFalse_WhenConfigFalse_EvenIfEnvTrue()
    {
        lock (s_envGate)
        {
            string? previous = Environment.GetEnvironmentVariable(EnvName);

            try
            {
                Environment.SetEnvironmentVariable(EnvName, "true");
                IConfiguration config = new ConfigurationBuilder()
                    .AddInMemoryCollection(
                        [new("ArchLucid:Persistence:AllowRlsBypass", "false")])
                    .Build();

                RlsBreakGlass.IsEnabled(config).Should().BeFalse();
            }
            finally
            {
                Environment.SetEnvironmentVariable(EnvName, previous);
            }
        }
    }

    [Fact]
    public void IsEnabled_ReturnsTrue_WhenEnvAndConfigTrue()
    {
        lock (s_envGate)
        {
            string? previous = Environment.GetEnvironmentVariable(EnvName);

            try
            {
                Environment.SetEnvironmentVariable(EnvName, "true");
                IConfiguration config = new ConfigurationBuilder()
                    .AddInMemoryCollection(
                        [new("ArchLucid:Persistence:AllowRlsBypass", "true")])
                    .Build();

                RlsBreakGlass.IsEnabled(config).Should().BeTrue();
            }
            finally
            {
                Environment.SetEnvironmentVariable(EnvName, previous);
            }
        }
    }

    [Fact]
    public void IsEnabled_Throws_WhenConfigurationNull()
    {
        Action act = () => RlsBreakGlass.IsEnabled(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("configuration");
    }
}
