using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;

namespace ArchLucid.Cli.Commands;

/// <summary><c>archlucid graph export</c> — fetches provenance graph JSON and emits Mermaid.</summary>
[ExcludeFromCodeCoverage(
    Justification = "HTTP + env auth against live API.")]
internal static class GraphExportCommand
{
    private static readonly JsonSerializerOptions JsonCamel = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task<int> RunAsync(string[] args)
    {
        string? runId = null;
        string format = "mermaid";
        string? decisionKey = null;
        string? outPath = null;
        ArchLucidProjectScaffolder.ArchLucidCliConfig? config = CliCommandShared.TryLoadConfigFromCwd();

        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];

            if (string.Equals(a, "--format", StringComparison.Ordinal) && i + 1 < args.Length)
            {


                format = args[++i].Trim().ToLowerInvariant();

                continue;
            }


            if (string.Equals(a, "--decision", StringComparison.Ordinal) && i + 1 < args.Length)
            {


                decisionKey = args[++i].Trim();


                continue;
            }


            if (string.Equals(a, "--out", StringComparison.Ordinal) && i + 1 < args.Length)
            {


                outPath = args[++i].Trim();


                continue;
            }

            if (!a.StartsWith("-", StringComparison.Ordinal) && runId is null)

            {


                runId = a.Trim();

                continue;
            }

            await Console.Error.WriteLineAsync(
                "Usage: archlucid graph export <runId> [--format mermaid] [--decision <key>] [--out <path>]");

            return CliExitCode.UsageError;
        }


        if (string.IsNullOrWhiteSpace(runId))


        {


            await Console.Error.WriteLineAsync(
                "Usage: archlucid graph export <runId> [--format mermaid] [--decision <key>] [--out <path>]");


            return CliExitCode.UsageError;
        }


        if (format != "mermaid")
        {


            await Console.Error.WriteLineAsync("Only --format mermaid is supported.");

            return CliExitCode.UsageError;
        }


        if (!Guid.TryParse(runId, out _))
        {


            await Console.Error.WriteLineAsync("runId must be a GUID (authority graph routes use v1 GUID paths).");

            return CliExitCode.UsageError;
        }


        string baseUrl = CliCommandShared.GetBaseUrl(config);
        ApiConnectionOutcome connection = await CliCommandShared.TryConnectToApiAsync(baseUrl, config);

        if (connection != ApiConnectionOutcome.Connected)
            return CliCommandShared.ExitCodeForFailedConnection(connection);


        Uri relativeUri = BuildRelativeGraphUri(runId, decisionKey);
        GraphWireModel graph;

        using (HttpClient http = ArchLucidApiClient.CreateSharedApiHttpClient(baseUrl, config))
        {
            using HttpResponseMessage response = await http.GetAsync(relativeUri);
            string bodyJson = await response.Content.ReadAsStringAsync();

            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)


            {


                await Console.Error.WriteLineAsync($"{(int)response.StatusCode} {response.StatusCode}: {TrimPreview(bodyJson, 480)}");


                return CliExitCode.OperationFailed;
            }


            response.EnsureSuccessStatusCode();


            graph = DeserializeGraph(bodyJson, response);


        }


        string mermaid = GraphWireMermaidFormatter.ToFlowchart(graph);

        if (!string.IsNullOrWhiteSpace(outPath))
        {
            await File.WriteAllTextAsync(Path.GetFullPath(outPath.Trim()), mermaid + Environment.NewLine);

            Console.WriteLine($"Wrote Mermaid ({graph.Nodes.Count} nodes, {graph.Edges.Count} edges) → {outPath}");

        }


        else

            Console.WriteLine(mermaid);


        return CliExitCode.Success;
    }

    private static string TrimPreview(string raw, int max)
    {


        raw = raw.ReplaceLineEndings(" ");

        return raw.Length <= max ? raw : raw[..max];
    }

    private static GraphWireModel DeserializeGraph(string bodyJson, HttpResponseMessage response)


    {


        using JsonDocument doc = JsonDocument.Parse(bodyJson);
        JsonElement root = doc.RootElement;
        JsonElement envelope = UnwrapGraphObject(root);


        GraphWireModel? mapped = envelope.Deserialize<GraphWireModel>(JsonCamel);


        return mapped ?? WarnEmpty(reason: $"Graph body deserialized to null ({response.RequestMessage?.RequestUri}).");

    }


    /// <remarks>Supports optional wrappers <c>data</c>/<c>payload</c> without tying the CLI to paging DTO names.</remarks>
    private static JsonElement UnwrapGraphObject(JsonElement root)
    {


        if (LooksLikeGraph(root))
            return root;


        foreach (string key in new[] { "data", "graph", "payload", "value" })


        {


            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty(key, out JsonElement nested))
                continue;


            if (LooksLikeGraph(nested))


                return nested;


        }


        return root;
    }


    private static bool LooksLikeGraph(JsonElement candidate)

    {


        return candidate.ValueKind == JsonValueKind.Object && candidate.TryGetProperty("nodes", out _) &&

               candidate.TryGetProperty("edges", out _);


    }


    private static GraphWireModel WarnEmpty(string reason)
    {


        Console.Error.WriteLine($"[graph export] Warning — {reason}");

        return new GraphWireModel();
    }

    private static Uri BuildRelativeGraphUri(string runGuid, string? decisionKey)
    {


        if (string.IsNullOrWhiteSpace(decisionKey))
            return new Uri($"v1/authority/runs/{runGuid}/graph", UriKind.Relative);


        return new Uri(
            $"v1/authority/runs/{runGuid}/graph/decision/{Uri.EscapeDataString(decisionKey)}",
            UriKind.Relative);


    }

}
