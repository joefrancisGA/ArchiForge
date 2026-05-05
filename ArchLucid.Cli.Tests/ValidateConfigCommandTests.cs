using System.Text;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Suite", "Configuration")]
public sealed class ValidateConfigCommandTests
{
    [Fact]
    public async Task ValidateConfig_EmptyDirectory_ReportsDatabaseError_Exit4()
    {
        string prevCwd = Environment.CurrentDirectory;

        string?[] keysToClear =
        [
            "ConnectionStrings__ArchLucid", "ASPNETCORE_ENVIRONMENT", "DOTNET_ENVIRONMENT", "ARCHLUCID_API_URL",
            "ARCHLUCID_API_KEY", "ArchLucid__StorageProvider", "AgentExecution__Mode"
        ];

        Dictionary<string, string?> saved = SaveClearEnv(keysToClear);

        string temp = Path.Combine(Path.GetTempPath(), "ArchLucid.Cli.Tests.validate." + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(temp);

        try
        {
            Environment.CurrentDirectory = temp;

            StringBuilder outSb = new();
            TextWriter prevO = Console.Out;

            try
            {
                Console.SetOut(new StringWriter(outSb));

                int exit = await Program.RunAsync(["validate-config"]);

                exit.Should().Be(CliExitCode.OperationFailed);
                string output = outSb.ToString();
                output.Should().Contain("[FAIL]");
                output.Should().Contain("ConnectionStrings:ArchLucid");
                output.Should().Contain("Error");
            }

            finally
            {
                Console.SetOut(prevO);
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
            }
        }
    }

    [Fact]
    public async Task ValidateConfig_JwtBearerWithoutAuthority_HasErrors()
    {
        string prevCwd = Environment.CurrentDirectory;

        Dictionary<string, string?> saved = SaveClearEnv(
            ["ConnectionStrings__ArchLucid", "ASPNETCORE_ENVIRONMENT", "DOTNET_ENVIRONMENT"]);

        string temp = Path.Combine(Path.GetTempPath(), "ArchLucid.Cli.Tests.validate." + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(temp);

        await File.WriteAllTextAsync(
            Path.Combine(temp, "appsettings.json"),
            """
            {
              "ArchLucidAuth": {
                "Mode": "JwtBearer",
                "Authority": "",
                "Audience": ""
              },
              "ConnectionStrings": {
                "ArchLucid": "Server=localhost;Database=ArchLucid;Trusted_Connection=True;"
              },
              "ArchLucid": { "StorageProvider": "Sql" }
            }
            """);

        try
        {
            Environment.CurrentDirectory = temp;

            StringBuilder outSb = new();
            TextWriter prevO = Console.Out;

            try
            {
                Console.SetOut(new StringWriter(outSb));

                int exit = await Program.RunAsync(["validate-config"]);

                exit.Should().Be(CliExitCode.OperationFailed);
                outSb.ToString().Should().Contain("ArchLucidAuth:Authority");
            }

            finally
            {
                Console.SetOut(prevO);
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
            }
        }
    }

    [Fact]
    public async Task ValidateConfig_InMemorySkippedSql_HasPassOutcome()
    {
        string prevCwd = Environment.CurrentDirectory;

        Dictionary<string, string?> saved = SaveClearEnv(
            ["ConnectionStrings__ArchLucid", "ASPNETCORE_ENVIRONMENT", "DOTNET_ENVIRONMENT"]);

        string temp = Path.Combine(Path.GetTempPath(), "ArchLucid.Cli.Tests.validate." + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(temp);

        await File.WriteAllTextAsync(
            Path.Combine(temp, "appsettings.json"),
            """
            {
              "ArchLucid": { "StorageProvider": "InMemory" },
              "ArchLucidAuth": { "Mode": "ApiKey" },
              "Authentication": { "ApiKey": { "Enabled": false } }
            }
            """);

        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development", EnvironmentVariableTarget.Process);

            Environment.CurrentDirectory = temp;

            StringBuilder outSb = new();
            TextWriter prevO = Console.Out;

            try
            {
                Console.SetOut(new StringWriter(outSb));

                int exit = await Program.RunAsync(["validate-config"]);

                exit.Should().Be(CliExitCode.Success);
                string text = outSb.ToString();
                text.Should().Contain("[PASS]").And.Contain("InMemory");
            }

            finally
            {
                Console.SetOut(prevO);
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
            }
        }
    }

    [Fact]
    public async Task ValidateConfig_JsonMode_WritesStructuredPayload_ToStdoutOnly()
    {
        string prevCwd = Environment.CurrentDirectory;

        Dictionary<string, string?> saved = SaveClearEnv(
            ["ConnectionStrings__ArchLucid", "ASPNETCORE_ENVIRONMENT", "DOTNET_ENVIRONMENT"]);

        string temp = Path.Combine(Path.GetTempPath(), "ArchLucid.Cli.Tests.validate." + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(temp);

        await File.WriteAllTextAsync(
            Path.Combine(temp, "appsettings.json"),
            """
            {
              "ArchLucid": { "StorageProvider": "InMemory" },
              "ArchLucidAuth": { "Mode": "ApiKey" },
              "Authentication": { "ApiKey": { "Enabled": false } }
            }
            """);

        try
        {
            Environment.CurrentDirectory = temp;

            StringBuilder outSb = new();
            StringBuilder errSb = new();
            TextWriter prevO = Console.Out;
            TextWriter prevE = Console.Error;

            try
            {
                Console.SetOut(new StringWriter(outSb));
                Console.SetError(new StringWriter(errSb));

                int exit = await Program.RunAsync(["--json", "validate-config"]);

                exit.Should().Be(CliExitCode.Success);
                outSb.ToString().Should().Contain("\"ok\":").And.Contain("\"findings\"").And.Contain("summary");
                errSb.ToString().Should().BeEmpty();
            }

            finally
            {
                Console.SetOut(prevO);
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
            }
        }
    }

    private static Dictionary<string, string?> SaveClearEnv(string?[] keys)
    {
        Dictionary<string, string?> saved = new(StringComparer.Ordinal);

        foreach (string? k in keys)
        {
            if (k is null)

                continue;

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
