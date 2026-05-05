using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ArchLucid.Cli.Commands;

[ExcludeFromCodeCoverage(Justification = "CLI demo-pack orchestration uses HTTP; integration-tested manually.")]
internal static class SeedDemoDataCommand
{
    private static readonly JsonSerializerOptions JsonCamel = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        ArchLucidProjectScaffolder.ArchLucidCliConfig? config = CliCommandShared.TryLoadConfigFromCwd();
        string baseUrl = CliCommandShared.GetBaseUrl(config);

        ApiConnectionOutcome connection = await CliCommandShared.TryConnectToApiAsync(baseUrl, config, cancellationToken);

        if (connection != ApiConnectionOutcome.Connected)
            return CliCommandShared.ExitCodeForFailedConnection(connection);

        using HttpClient http = new();
        http.Timeout = TimeSpan.FromSeconds(120);
        http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        string? apiKey = Environment.GetEnvironmentVariable("ARCHLUCID_API_KEY");

        if (!string.IsNullOrWhiteSpace(apiKey))
            http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        (bool ok, string msg) seedDemo = await PostEmptyAsync(http, "v1/demo/seed", cancellationToken);

        (bool ok, string msg, int? written) markers =
            await PostSyntheticMarkersAsync(http, cancellationToken);

        if (CliExecutionContext.JsonOutput)
        {
            object payload = new
            {
                ok = seedDemo.ok || markers.ok,
                demoSeed = seedDemo.msg,
                syntheticMarkers = markers.msg,
                auditEventsWritten = markers.written
            };
            Console.WriteLine(JsonSerializer.Serialize(payload, JsonCamel));
        }
        else
        {
            Console.WriteLine("archlucid seed-demo-data");
            Console.WriteLine($"  Demo seed POST /v1/demo/seed: {seedDemo.msg}");
            Console.WriteLine($"  Synthetic markers POST /v1/diagnostics/synthetic-operator-demo-pack: {markers.msg}");
            Console.WriteLine(
                "  Next: open operator Home — audit markers include DataJson.syntheticDemoPack=true (filter in /audit).");
            Console.WriteLine(
                "  For three committed runs + full Contoso depth, enable Demo:Enabled and Development (or run `archlucid try`).");
        }

        return seedDemo.ok || markers.ok ? CliExitCode.Success : CliExitCode.OperationFailed;
    }

    private static async Task<(bool ok, string msg)> PostEmptyAsync(
        HttpClient http,
        string relative,
        CancellationToken ct)
    {
        try
        {
            using HttpResponseMessage response = await http.PostAsync(
                relative,
                new StringContent(string.Empty, Encoding.UTF8, "application/json"),
                ct);

            string line = response.StatusCode switch
            {
                HttpStatusCode.NoContent => "OK (204)",
                HttpStatusCode.BadRequest => "Demo disabled or misconfigured (400)",
                HttpStatusCode.Forbidden => "Forbidden — use operator API key with Execute (403)",
                HttpStatusCode.NotFound => "Unavailable in this environment (404)",
                _ => $"HTTP {(int)response.StatusCode}"
            };

            return (response.IsSuccessStatusCode, line);
        }
        catch (Exception ex)
        {
            return (false, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<(bool ok, string msg, int? written)> PostSyntheticMarkersAsync(
        HttpClient http,
        CancellationToken ct)
    {
        try
        {
            using HttpResponseMessage response = await http.PostAsync(
                "v1/diagnostics/synthetic-operator-demo-pack",
                new StringContent("{}", Encoding.UTF8, "application/json"),
                ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return (false, "Not available (needs Demo:Enabled or Development host + Admin API key)", null);

            if (response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized)
                return (false, "Unauthorized — set ARCHLUCID_API_KEY to an Admin key", null);

            string body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                return (false, $"HTTP {(int)response.StatusCode}: {body}", null);

            using JsonDocument doc = JsonDocument.Parse(body);
            int written = doc.RootElement.TryGetProperty("auditEventsWritten", out JsonElement w) ? w.GetInt32() : 0;

            return (true, $"OK — wrote {written} marker audit rows", written);
        }
        catch (Exception ex)
        {
            return (false, $"{ex.GetType().Name}: {ex.Message}", null);
        }
    }
}
