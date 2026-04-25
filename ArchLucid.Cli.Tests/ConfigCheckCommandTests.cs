using System.Text;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

/// <summary>Integration-style tests for <c>archlucid config check</c> (no live API — <c>--no-api</c>).</summary>
[Trait("Suite", "Configuration")]
public sealed class ConfigCheckCommandTests
{
  [Fact]
  public async Task ConfigCheck_NoApi_EmptyConfig_MarksSqlConnectionMissing_Exit4()
  {
    string? prevCwd = Environment.CurrentDirectory;
    string?[] keysToClear =
    [
      "ConnectionStrings__ArchLucid", "ASPNETCORE_ENVIRONMENT", "DOTNET_ENVIRONMENT", "ARCHLUCID_API_URL",
      "ARCHLUCID_API_KEY", "ArchLucid__StorageProvider"
    ];
    Dictionary<string, string?> saved = new(StringComparer.Ordinal);
    foreach (string? k in keysToClear)
    {
      if (k is null)
      {
        continue;
      }

      saved[k] = Environment.GetEnvironmentVariable(k);
      Environment.SetEnvironmentVariable(k, null, EnvironmentVariableTarget.Process);
    }

    string temp = Path.Combine(Path.GetTempPath(), "ArchLucid.Cli.Tests.config." + Guid.NewGuid().ToString("N")[..8]);
    Directory.CreateDirectory(temp);
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
        int exit = await Program.RunAsync(["config", "check", "--no-api"]);
        exit
          .Should()
          .Be(CliExitCode.OperationFailed, "required SQL connection is missing in empty config (exit 4)");
        string combined = outSb + errSb.ToString();
        combined
          .Should()
          .Contain("ConnectionStrings:ArchLucid");
        combined
          .Should()
          .Contain("MISSING");
        combined
          .Should()
          .Contain("Note: --no-api", "local-only check must explain API snapshot skipped");
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
      try
      {
        Directory.Delete(temp, true);
      }
      catch
      {
        // best-effort cleanup
      }

      foreach (KeyValuePair<string, string?> kvp in saved)
      {
        Environment.SetEnvironmentVariable(
          kvp.Key, kvp.Value, EnvironmentVariableTarget.Process);
      }
    }
  }
}
