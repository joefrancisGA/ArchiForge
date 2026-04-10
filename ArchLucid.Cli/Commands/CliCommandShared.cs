using System.Text.Json;

using ArchLucid.Cli.Support;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;

namespace ArchLucid.Cli.Commands;

/// <summary>JSON options, config loading, and API connectivity helpers shared by CLI commands.</summary>
internal static class CliCommandShared
{
    internal static readonly JsonSerializerOptions JsonWriteIndented = new() { WriteIndented = true };

    internal static readonly JsonSerializerOptions JsonDeserializeAgentResult = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    internal static ArchLucidProjectScaffolder.ArchLucidCliConfig? TryLoadConfigFromCwd()
    {
        try
        {
            return ArchLucidProjectScaffolder.LoadConfig(Directory.GetCurrentDirectory());
        }
        catch
        {
            return null;
        }
    }

    internal static string GetBaseUrl(ArchLucidProjectScaffolder.ArchLucidCliConfig? config) =>
        ArchLucidApiClient.ResolveBaseUrl(config);

    internal static async Task<bool> EnsureApiConnectedAsync(
        string baseUrl,
        ArchLucidProjectScaffolder.ArchLucidCliConfig? config = null,
        CancellationToken ct = default)
    {
        string? urlError = ArchLucidApiClient.GetInvalidApiBaseUrlReason(baseUrl);

        if (urlError is not null)
        {
            await Console.Error.WriteLineAsync("[ArchLucid CLI] " + urlError);

            return false;
        }

        ArchLucidApiClient client = new(baseUrl, config);

        if (await client.CheckHealthAsync(ct))
        {
            return true;
        }

        Console.WriteLine($"Cannot connect to ArchLucid API at {baseUrl}");
        Console.WriteLine("Ensure the API is running: dotnet run --project ArchLucid.Api");
        Console.WriteLine("Or set apiUrl in archlucid.json / ARCHLUCID_API_URL environment variable.");
        CliOperatorHints.WriteAfterHealthUnreachable(baseUrl);

        return false;
    }

    internal static ArchitectureRequest BuildArchitectureRequest(
        ArchLucidProjectScaffolder.ArchLucidCliConfig config,
        string briefContent)
    {
        ArchLucidProjectScaffolder.ArchitectureSection? arch = config.Architecture;

        ArchitectureRequest request = new()
        {
            RequestId = Guid.NewGuid().ToString("N"),
            SystemName = config.ProjectName,
            Description = briefContent,
            Environment = arch?.Environment ?? "prod",
            CloudProvider = ParseCloudProvider(arch?.CloudProvider),
            Constraints = arch?.Constraints ?? [],
            RequiredCapabilities = arch?.RequiredCapabilities ?? [],
            Assumptions = arch?.Assumptions ?? [],
            PriorManifestVersion = arch?.PriorManifestVersion
        };

        return request;
    }

    internal static CloudProvider ParseCloudProvider(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return CloudProvider.Azure;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            _ => CloudProvider.Azure
        };
    }

    internal static void WriteRunSummary(
        string path,
        string apiBaseUrl,
        string runId,
        string requestId,
        int status,
        DateTime createdUtc,
        IReadOnlyList<ArchLucidApiClient.AgentTaskInfo> tasks,
        string? manifestVersion)
    {
#pragma warning disable IDE0300 // Simplify collection initialization
#pragma warning disable IDE0301 // Simplify collection initialization
        object summary = new
        {
            runId,
            requestId,
            status,
            createdUtc = createdUtc.ToString("O"),
            manifestVersion,
            apiBaseUrl,
            tasks = tasks.Select(t => new { t.TaskId, agentType = (AgentType)t.AgentType, t.Objective }).ToArray(),
            artifactUris = manifestVersion != null
                ? new[] { $"{apiBaseUrl}/v1/architecture/manifest/{manifestVersion}" }
                : Array.Empty<string>()
        };
#pragma warning restore IDE0301 // Simplify collection initialization
#pragma warning restore IDE0300 // Simplify collection initialization
        string json = JsonSerializer.Serialize(summary, JsonWriteIndented);
        File.WriteAllText(path, json);
    }
}
