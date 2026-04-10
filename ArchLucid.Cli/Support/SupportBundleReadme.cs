namespace ArchLucid.Cli.Support;

/// <summary>
/// Plain-text index for pilots who open the folder before reading JSON (reduces archaeology).
/// </summary>
public static class SupportBundleReadme
{
    /// <summary>Written as <see cref="SupportBundleArchiveWriter.ReadmeFileName"/> next to JSON sections.</summary>
    public static string Build(string createdUtcIso, string apiBaseUrlRedacted, string cliWorkingDirectory)
    {
        return $"""
            ArchLucid support bundle
            ========================
            Generated (UTC): {createdUtcIso}
            CLI working directory: {cliWorkingDirectory}
            API base URL (redacted): {apiBaseUrlRedacted}

            Read first (in order)
            ---------------------
            1. health.json       — /health/live, /health/ready, /health (why the API may be unhealthy)
            2. build.json        — CLI build + GET /version (server build identity)
            3. api-contract.json — GET /openapi/v1.json probe (contract endpoint up; body is truncated)
            4. config-summary.json — archlucid.json summary (paths only; no secrets)
            5. environment.json — safe env snapshot (sensitive names show (set)/(not set) only)
            6. workspace.json  — outputs folder stats
            7. logs.json         — optional local last-run excerpt
            8. references.json   — doc links and correlation triage hints

            manifest.json includes triageReadOrder (same as above) and bundleFormatVersion.

            Before you send this folder or zip externally: review all JSON; policy may require extra redaction.

            Correlation: use X-Correlation-ID / correlationId from API errors with server logs (see references.json).

            """;
    }
}
