using System.Text;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

/// <summary>Integration-style exercises for <c>archlucid config lint</c>.</summary>
[Trait("Suite", "Configuration")]
public sealed class ConfigLintCommandTests
{
    [Fact]
    public async Task ConfigLint_WithDevelopmentAspnetAndEmptyAppsettings_ReturnsSuccess()
    {
        string prevCwd = Environment.CurrentDirectory;

        Dictionary<string, string?> saved = SaveClearEnv(
          ["ASPNETCORE_ENVIRONMENT", "DOTNET_ENVIRONMENT", "ARCHLUCID_ENVIRONMENT", "ARCHLUCID_API_URL"]);

        string temp =
            Path.Combine(Path.GetTempPath(), "ArchLucid.Cli.Tests.configLint." + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(temp);

        await File.WriteAllTextAsync(Path.Combine(temp, "appsettings.json"), "{}");

        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development", EnvironmentVariableTarget.Process);
            Environment.CurrentDirectory = temp;

            StringBuilder errSb = new();
            TextWriter prevE = Console.Error;

            try
            {
                Console.SetError(new StringWriter(errSb));

                int exit = await Program.RunAsync(["config", "lint"]);

                exit.Should().Be(CliExitCode.Success, errSb.ToString());
            }

            finally
            {
                Console.SetError(prevE);
            }
        }

        finally
        {
            Environment.CurrentDirectory = prevCwd;
            RestoreEnv(saved);

            try
            {
                Directory.Delete(temp, true);
            }

            catch
            {
                // best-effort cleanup
            }
        }
    }

    [Fact]
    public async Task ConfigLint_SimulateProduction_DevelopmentBypassIsBlocked()
    {
        string prevCwd = Environment.CurrentDirectory;

        Dictionary<string, string?> saved = SaveClearEnv(
          ["ASPNETCORE_ENVIRONMENT", "DOTNET_ENVIRONMENT", "ARCHLUCID_ENVIRONMENT", "ARCHLUCID_API_URL"]);

        string temp =
            Path.Combine(Path.GetTempPath(), "ArchLucid.Cli.Tests.configLint." + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(temp);

        await File.WriteAllTextAsync(
            Path.Combine(temp, "appsettings.json"),
            "{\"ArchLucidAuth\":{\"Mode\":\"DevelopmentBypass\"}}");

        try
        {
            Environment.CurrentDirectory = temp;

            StringBuilder errSb = new();
            TextWriter prevE = Console.Error;

            try
            {
                Console.SetError(new StringWriter(errSb));

                int exit = await Program.RunAsync(["config", "lint", "--simulate-production"]);

                exit.Should().Be(CliExitCode.OperationFailed, errSb.ToString());
                errSb.ToString().Should().Contain("DevelopmentBypass").And.Contain("ASPNETCORE_ENVIRONMENT");
            }

            finally
            {
                Console.SetError(prevE);
            }
        }

        finally
        {
            Environment.CurrentDirectory = prevCwd;
            RestoreEnv(saved);

            try
            {
                Directory.Delete(temp, true);
            }

            catch
            {
                // best-effort cleanup
            }
        }
    }

    private static Dictionary<string, string?> SaveClearEnv(string[] keys)
    {
        Dictionary<string, string?> saved = new(StringComparer.Ordinal);

        foreach (string k in keys)
        {
            saved[k] = Environment.GetEnvironmentVariable(k);
            Environment.SetEnvironmentVariable(k, null, EnvironmentVariableTarget.Process);
        }

        return saved;
    }

    private static void RestoreEnv(IReadOnlyDictionary<string, string?> saved)
    {
        foreach (KeyValuePair<string, string?> kvp in saved)
            Environment.SetEnvironmentVariable(kvp.Key, kvp.Value, EnvironmentVariableTarget.Process);
    }
}
