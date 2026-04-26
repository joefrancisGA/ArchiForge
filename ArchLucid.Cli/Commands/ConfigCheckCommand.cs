using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Core.Configuration;
using ArchLucid.Core.Configuration.Summary;

using Microsoft.Extensions.Configuration;

namespace ArchLucid.Cli.Commands;

[ExcludeFromCodeCoverage(Justification = "Thin I/O; Core + tests cover logic.")]
internal static class ConfigCheckCommand
{
    private static readonly JsonSerializerOptions JsonWriter = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        bool noApi = args.Any(
          a => string.Equals(a, "--no-api", StringComparison.Ordinal));
        ArchLucidProjectScaffolder.ArchLucidCliConfig? cli = CliCommandShared.TryLoadConfigFromCwd();
        IConfiguration local = BuildLocalConfiguration(cli);
        string? envName =
          Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
          ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        IReadOnlyDictionary<string, bool>? apiMap;
        string? apiNote;
        if (noApi)
        {
            apiMap = null;
            apiNote = "Note: --no-api — only local file + environment (no server snapshot).";
        }
        else
        {
            (apiMap, apiNote) = await TryFetchApiSummaryAsync(cli, cancellationToken).ConfigureAwait(false);
        }

        IReadOnlyList<ConfigurationKeyEntry> allKeys = ConfigurationKeyCatalog.All
          .Concat(ConfigurationKeyCatalog.CliLocalOnly)
          .ToList();
        HashSet<string> cliOnly = new(
          ConfigurationKeyCatalog.CliLocalOnly
            .Select(s => s.ConfigPath), StringComparer.OrdinalIgnoreCase);
        int requiredTotal = 0;
        int requiredSatisfied = 0;
        int optionalTotal = 0;
        int optionalSet = 0;
        List<ConfigCheckLine> lines = new(allKeys.Count + 1);
        foreach (ConfigurationKeyEntry e in allKeys)
        {
            (bool fromApi, bool fromLocal) = SplitPresence(
              e.ConfigPath,
              local,
              apiMap,
              cliOnly);
            bool isSet = fromApi || fromLocal;
            string source = FormatSource(fromApi, fromLocal);
            bool isRequired = ConfigurationKeyRequirement.IsKeyRequired(e, local, envName, out _);
            if (isRequired)
            {
                requiredTotal++;
                if (isSet)
                {
                    requiredSatisfied++;
                }
            }
            else
            {
                optionalTotal++;
                if (isSet)
                {
                    optionalSet++;
                }
            }

            lines.Add(new ConfigCheckLine(e.ConfigPath, isSet, source, isRequired, e.Description));
        }

        bool pairFailed = false;
        if (local.GetValue("Authentication:ApiKey:Enabled", false))
        {
            bool hAdmin = ConfigurationKeyPresence.IsValuePresent(local, "Authentication:ApiKey:AdminKey");
            bool hRead = ConfigurationKeyPresence.IsValuePresent(local, "Authentication:ApiKey:ReadOnlyKey");
            if (!hAdmin && !hRead)
            {
                pairFailed = true;
                requiredTotal++;
                lines.Add(
                  new ConfigCheckLine(
                    "ApiKey(Admin|Read) pair",
                    false,
                    "required-rule",
                    true,
                    "At least one of Authentication:ApiKey:AdminKey or ReadOnlyKey when API key mode is on."));
            }
        }

        int anyMissing = 0;
        foreach (ConfigCheckLine c in lines)
        {
            if (c is { IsRequired: true, IsSet: false })
            {
                anyMissing++;
            }
        }

        bool ok = anyMissing == 0 && !pairFailed;
        if (CliExecutionContext.JsonOutput)
        {
            var payload = new
            {
                ok,
                hasApiKeySnapshot = apiMap is not null,
                note = apiNote,
                summary = new
                {
                    requiredSatisfied,
                    requiredTotal,
                    optionalSet,
                    optionalTotal
                },
                keys = lines
                .Select(
                  c => new
                  {
                      configPath = c.ConfigPath,
                      c.IsSet,
                      c.Source,
                      c.IsRequired,
                      c.Notes
                  })
                .ToList()
            };
            Console.WriteLine(JsonSerializer.Serialize(payload, JsonWriter));
        }
        else
        {
            if (apiNote is not null)
            {
                Console.WriteLine(apiNote);
                Console.WriteLine();
            }

            foreach (ConfigCheckLine c in lines)
            {
                string m = c.IsSet ? "SET" : "MISSING";
                Console.WriteLine(
                  $"{c.ConfigPath,-60} {m,-8} {c.Source,-16} {(c.IsRequired ? "req" : "opt")} {c.Notes}");
            }

            Console.WriteLine();
            Console.WriteLine(
              $"Required satisfied: {requiredSatisfied}/{requiredTotal} · optional set: {optionalSet}/{optionalTotal} (optional do not fail the command).");
            if (pairFailed)
                await Console.Error.WriteLineAsync("API key key material: set AdminKey and/or ReadOnlyKey when `Authentication:ApiKey:Enabled` is true.");
        }

        return ok ? CliExitCode.Success : CliExitCode.OperationFailed;
    }

    private static IConfiguration BuildLocalConfiguration(ArchLucidProjectScaffolder.ArchLucidCliConfig? cli)
    {
        List<KeyValuePair<string, string?>> m = new(2);
        if (cli is not null && !string.IsNullOrWhiteSpace(cli.ApiUrl))
        {
            m.Add(
              new KeyValuePair<string, string?>("ARCHLUCID_API_URL", cli.ApiUrl.Trim().TrimEnd('/')));
        }

        IConfigurationBuilder b = new ConfigurationBuilder()
          .SetBasePath(Directory.GetCurrentDirectory())
          .AddJsonFile("archlucid.json", true, true)
          .AddJsonFile("appsettings.json", true, true)
          .AddInMemoryCollection(m)
          .AddEnvironmentVariables();

        return b.Build();
    }

    private static (bool fromApi, bool fromLocal) SplitPresence(
      string path,
      IConfiguration local,
      IReadOnlyDictionary<string, bool>? apiMap,
      HashSet<string> cliOnly)
    {
        bool fromApi = false;
        if (apiMap is not null
            && !cliOnly.Contains(path)
            && apiMap.TryGetValue(
              path, out bool a))
        {
            fromApi = a;
        }

        bool fromLocal;
        if (string.Equals(
                path, "ASPNETCORE_ENVIRONMENT", StringComparison.Ordinal) || string.Equals(
                path, "DOTNET_ENVIRONMENT", StringComparison.Ordinal)
            )
        {
            fromLocal = !string.IsNullOrWhiteSpace(
              Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
              ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"));
        }
        else if (string.Equals(path, "ARCHLUCID_API_KEY", StringComparison.Ordinal))
        {
            fromLocal = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ARCHLUCID_API_KEY"));
        }
        else if (string.Equals(path, "ARCHLUCID_API_URL", StringComparison.Ordinal))
        {
            fromLocal = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ARCHLUCID_API_URL"))
              || !string.IsNullOrWhiteSpace(local["ARCHLUCID_API_URL"]);
        }
        else
        {
            fromLocal = ConfigurationKeyPresence.IsValuePresent(local, path);
        }

        return (fromApi, fromLocal);
    }

    private static string FormatSource(bool fromApi, bool fromLocal)
    {
        if (fromApi && fromLocal)
        {
            return "api+local";
        }

        if (fromApi)
        {
            return "api";
        }

        return fromLocal ? "local" : "—";
    }

    private static async Task<(IReadOnlyDictionary<string, bool>?, string?)> TryFetchApiSummaryAsync(
      ArchLucidProjectScaffolder.ArchLucidCliConfig? config,
      CancellationToken cancellationToken)
    {
        string baseUrl = ArchLucidApiClient.ResolveBaseUrl(config);
        if (ArchLucidApiClient.GetInvalidApiBaseUrlReason(baseUrl) is { } err)
        {
            return (null, "API: (skip) " + err);
        }

        string? k = Environment.GetEnvironmentVariable("ARCHLUCID_API_KEY");
        if (string.IsNullOrWhiteSpace(k))
        {
            return (null, "API: (skip) set ARCHLUCID_API_KEY (Admin) to merge GET /v1/admin/config-summary presence.");
        }

        using HttpClient c = new();
        c.BaseAddress = new Uri(
            baseUrl
                .Trim()
                .TrimEnd('/') + "/", UriKind.Absolute);
        c.Timeout = TimeSpan.FromSeconds(20);
        c.DefaultRequestHeaders.Add("X-Api-Key", k);
        c.DefaultRequestHeaders.Add("Accept", "application/json");

        try
        {
            using HttpResponseMessage r = await c
              .GetAsync("v1/admin/config-summary", cancellationToken)
              .ConfigureAwait(false);
            if (r.StatusCode == HttpStatusCode.Unauthorized)
            {
                return (null, "API: 401 (Admin).");
            }

            if (r.StatusCode == HttpStatusCode.NotFound)
            {
                return (null, "API: 404 (this server build has no /v1/admin/config-summary).");
            }

            r.EnsureSuccessStatusCode();
            string body = await r.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            AdminConfigSummaryResponse? d = JsonSerializer.Deserialize<AdminConfigSummaryResponse>(
              body, new JsonSerializerOptions
              {
                  PropertyNameCaseInsensitive = true,
                  PropertyNamingPolicy = JsonNamingPolicy.CamelCase
              });
            if (d?.Keys is not { } rows || rows.Count == 0)
                return (null, "API: (skip) empty body");

            IReadOnlyDictionary<string, bool> m = rows
              .Where(static r => !string.IsNullOrEmpty(r.ConfigPath))
              .ToDictionary(
                static r => r.ConfigPath!, static r => r.IsSet, StringComparer.OrdinalIgnoreCase);
            return (m, "API: merged key presence (non-secret) from GET /v1/admin/config-summary.");
        }
        catch (Exception ex)
        {
            return (null, "API: (skip) " + ex.GetType().Name);
        }
    }
}
