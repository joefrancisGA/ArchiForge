namespace ArchiForge.Cli;

/// <summary>
/// Concise stderr hints after CLI API failures (aligned with docs/TROUBLESHOOTING.md).
/// </summary>
internal static class CliOperatorHints
{
    /// <summary>After a failed HTTP API call from the CLI (status from response, or null on transport errors).</summary>
    public static void WriteAfterApiFailure(int? httpStatusCode, string? errorMessage, TextWriter? stderr = null)
    {
        stderr ??= Console.Error;

        if (!string.IsNullOrEmpty(errorMessage) &&
            errorMessage.Contains("Cannot connect to ArchiForge API", StringComparison.OrdinalIgnoreCase))
        {
            stderr.WriteLine(
                "Next: Start the API (dotnet run --project ArchiForge.Api), set ARCHIFORGE_API_URL or apiUrl in archiforge.json, and confirm the port is reachable.");

            return;
        }

        if (!string.IsNullOrEmpty(errorMessage) &&
            errorMessage.Contains("Request timed out", StringComparison.OrdinalIgnoreCase))
        {
            stderr.WriteLine(
                "Next: Check API and SQL responsiveness, network path, and try again. See GET /health/ready on the API host.");

            return;
        }

        string? line = LineForHttpStatus(httpStatusCode);
        if (!string.IsNullOrEmpty(line))
        
            stderr.WriteLine(line);
        
    }

    public static void WriteAfterHealthUnreachable(string baseUrl, TextWriter? stderr = null)
    {
        stderr ??= Console.Error;
        stderr.WriteLine(
            $"Next: Verify the API is listening at {baseUrl}, SQL/migrations succeeded, and GET /health/ready returns 200. Run: dotnet run --project ArchiForge.Cli -- doctor");
    }

    public static void WriteAfterReadinessFailed(TextWriter? stderr = null)
    {
        stderr ??= Console.Error;
        stderr.WriteLine(
            "Next: Open GET /health/ready in a browser or curl and fix the failing check (often ConnectionStrings:ArchiForge or DbUp). Capture GET /version for build identity when opening a ticket. See docs/TROUBLESHOOTING.md.");
    }

    public static void WriteBriefMissingHint(string relativeBriefPath, TextWriter? stderr = null)
    {
        stderr ??= Console.Error;
        stderr.WriteLine(
            $"Next: Create the file at {relativeBriefPath} (minimum 10 characters) or update inputs.brief in archiforge.json.");
    }

    private static string? LineForHttpStatus(int? code) =>
        code switch
        {
            null => null,
            Status401 => "Next: Configure authentication (ArchiForgeAuth / JWT) or ARCHIFORGE_API_KEY when the API requires it.",
            Status403 => "Next: Use an identity with Reader, Operator, or Admin as mapped by your deployment.",
            Status404 => "Next: Verify IDs and scope headers (x-tenant-id, x-workspace-id, x-project-id) match the resource.",
            Status409 => "Next: Read the API detail; you may need a fresh run or to resolve idempotency/state.",
            Status422 or Status400 => "Next: Fix the request body using the API problem detail.",
            Status429 => "Next: Wait and retry, or relax RateLimiting in non-production.",
            Status503 => "Next: GET /health/ready on the API; SQL availability and migrations are common causes.",
            >= Status500 => "Next: Retry once; if it persists, use the response correlation ID and server logs (RunId when present).",
            _ => null
        };

    private const int Status400 = 400;
    private const int Status401 = 401;
    private const int Status403 = 403;
    private const int Status404 = 404;
    private const int Status409 = 409;
    private const int Status422 = 422;
    private const int Status429 = 429;
    private const int Status503 = 503;
    private const int Status500 = 500;
}
