using System.Diagnostics;

namespace ArchLucid.Cli.Commands;

/// <summary>
/// Resolves the persisted creation trace id for a run and prints or opens the operator trace viewer URL.
/// </summary>
internal static class TraceCommand
{
    private const string TraceViewerTemplateEnv = "ARCHLUCID_TRACE_VIEWER_URL_TEMPLATE";
    private const string OpenBrowserEnv = "ARCHLUCID_TRACE_OPEN_BROWSER";

    /// <summary>Entry point: connect to API, fetch run, print trace link or guidance.</summary>
    public static async Task<int> RunAsync(string runId, CancellationToken cancellationToken = default)
    {
        ArchLucidProjectScaffolder.ArchLucidCliConfig? config = CliCommandShared.TryLoadConfigFromCwd();
        string baseUrl = CliCommandShared.GetBaseUrl(config);

        if (!await CliCommandShared.EnsureApiConnectedAsync(baseUrl, config, cancellationToken))
        {
            return 1;
        }

        ArchLucidApiClient client = new(baseUrl, config);

        return await RunCoreAsync(
            runId,
            ct => client.GetRunAsync(runId, ct),
            () => Environment.GetEnvironmentVariable(TraceViewerTemplateEnv),
            ReadOpenBrowserEnv,
            Console.Out,
            TryOpenTraceInBrowser,
            cancellationToken);
    }

    /// <summary>Test hook: avoids live API and console.</summary>
    internal static async Task<int> RunCoreAsync(
        string runId,
        Func<CancellationToken, Task<ArchLucidApiClient.GetRunResult?>> fetchRun,
        Func<string?> getTraceViewerTemplate,
        Func<bool> shouldOpenBrowser,
        TextWriter output,
        Action<string>? openBrowser,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(runId);
        ArgumentNullException.ThrowIfNull(fetchRun);
        ArgumentNullException.ThrowIfNull(getTraceViewerTemplate);
        ArgumentNullException.ThrowIfNull(shouldOpenBrowser);
        ArgumentNullException.ThrowIfNull(output);

        ArchLucidApiClient.GetRunResult? detail = await fetchRun(cancellationToken);

        if (detail is null)
        {
            await output.WriteLineAsync(
                $"Run '{runId}' not found. Ensure the ArchLucid API is running and the id is correct.");

            return 1;
        }

        string? traceId = detail.Run.OtelTraceId;

        if (string.IsNullOrWhiteSpace(traceId))
        {
            await output.WriteLineAsync(
                $"No trace ID recorded for run {runId}. The run may predate trace persistence.");

            return 0;
        }

        string? template = getTraceViewerTemplate();

        if (string.IsNullOrWhiteSpace(template))
        {
            await output.WriteLineAsync(traceId);
            await output.WriteLineAsync(
                "Set ARCHLUCID_TRACE_VIEWER_URL_TEMPLATE to enable direct links (e.g., https://grafana.example.com/explore?traceId={traceId}).");

            return 0;
        }

        string url = BuildTraceViewerUrl(template, traceId);
        await output.WriteLineAsync(url);

        if (shouldOpenBrowser() && openBrowser is not null)
        {
            openBrowser(url);
        }

        return 0;
    }

    /// <summary>Matches <c>archlucid-ui</c> <c>trace-link.ts</c>: template with <c>{traceId}</c> placeholder, trace id URL-encoded.</summary>
    internal static string BuildTraceViewerUrl(string template, string traceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template);
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);

        string encoded = Uri.EscapeDataString(traceId);

        return template.Replace("{traceId}", encoded, StringComparison.Ordinal);
    }

    internal static bool ReadOpenBrowserEnv()
    {
        string? value = Environment.GetEnvironmentVariable(OpenBrowserEnv);

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Equals("1", StringComparison.OrdinalIgnoreCase)
            || value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private static void TryOpenTraceInBrowser(string url)
    {
        try
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true,
                });
        }
        catch
        {
            // Best-effort: URL was already printed.
        }
    }
}
