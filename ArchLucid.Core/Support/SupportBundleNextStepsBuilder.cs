namespace ArchLucid.Core.Support;

/// <summary>
///     Builds conservative, probe-driven triage text for support bundles (API host and CLI client).
/// </summary>
public static class SupportBundleNextStepsBuilder
{
    private const string TroubleshootingDoc = "docs/TROUBLESHOOTING.md";

    private const string OperatorQuickstartDoc = "docs/OPERATOR_QUICKSTART.md";

    private const string PendingQuestionsDoc = "docs/PENDING_QUESTIONS.md item 37";

    /// <summary>API host ZIP (no HTTP probes — host process snapshot only).</summary>
    public static SupportBundleNextStepsDocument BuildForApiHost(
        string generatedAtUtcIso,
        IReadOnlyDictionary<string, string> archlucidAndDotnetEnvironment)
    {
        if (string.IsNullOrWhiteSpace(generatedAtUtcIso))
            throw new ArgumentException("Value must be provided.", nameof(generatedAtUtcIso));
        if (archlucidAndDotnetEnvironment is null)
            throw new ArgumentNullException(nameof(archlucidAndDotnetEnvironment));

        List<SupportBundleNextStepHint> hints = [];
        List<string> lines = [];

        if (archlucidAndDotnetEnvironment.TryGetValue("ARCHLUCID_API_URL", out string? apiUrl) &&
            string.IsNullOrWhiteSpace(apiUrl))
        {
            SupportBundleNextStepHint hint = new()
            {
                Id = "archlucid-api-url-empty",
                Severity = "warning",
                Message =
                    "ARCHLUCID_API_URL is present but empty in the captured host environment — outbound automation from this host may lack a target URL.",
                DocReference = OperatorQuickstartDoc,
            };

            hints.Add(hint);
            lines.Add(hint.Message);
        }

        if (archlucidAndDotnetEnvironment.TryGetValue("DOTNET_ENVIRONMENT", out string? dotnetEnv)
            && string.Equals(dotnetEnv.Trim(), "Production", StringComparison.OrdinalIgnoreCase))
        {
            SupportBundleNextStepHint hint = new()
            {
                Id = "dotnet-environment-production",
                Severity = "info",
                Message = "DOTNET_ENVIRONMENT is Production on this host.",
                DocReference = TroubleshootingDoc,
            };

            hints.Add(hint);
            lines.Add(
                "Host process reports DOTNET_ENVIRONMENT=Production — ensure reported symptoms are not from a misconfigured non-production expectation.");
        }

        lines.Add(
            "Attach this ZIP to your support thread and include the X-Correlation-ID from any failing API response (see references.json).");
        lines.Add(
            "Compare build.json against the CLI or client version reporting the issue to rule out a version skew.");
        lines.Add(
            $"Review environment.json and {PendingQuestionsDoc} before forwarding this bundle outside your organization.");

        return new SupportBundleNextStepsDocument
        {
            Source = "api", GeneratedUtc = generatedAtUtcIso, SummaryLines = lines, Hints = hints,
        };
    }

    /// <summary>CLI ZIP (includes HTTP probes against the configured API base URL).</summary>
    public static SupportBundleNextStepsDocument BuildForCliClient(
        string generatedAtUtcIso,
        int healthLiveStatus,
        int healthReadyStatus,
        int healthCombinedStatus,
        int openApiHttpStatus,
        string? apiVersionProbeError,
        bool archlucidJsonPresent,
        bool hasLocalLogExcerpt)
    {
        if (string.IsNullOrWhiteSpace(generatedAtUtcIso))
            throw new ArgumentException("Value must be provided.", nameof(generatedAtUtcIso));

        List<SupportBundleNextStepHint> hints = [];
        List<string> lines = [];

        if (healthLiveStatus == 0)
        {
            SupportBundleNextStepHint h = new()
            {
                Id = "health-live-transport",
                Severity = "action",
                Message =
                    "GET /health/live did not return an HTTP status (transport error). Verify ARCHLUCID_API_URL / archlucid.json, TLS trust, VPN, and that the API is listening.",
                DocReference = TroubleshootingDoc,
            };

            hints.Add(h);
            lines.Add(h.Message);
        }
        else if (healthLiveStatus != 200)
        {
            SupportBundleNextStepHint h = new()
            {
                Id = "health-live-http-non-200",
                Severity = "action",
                Message =
                    $"GET /health/live returned HTTP {healthLiveStatus}. Open health.json for the response body and see {TroubleshootingDoc}.",
                DocReference = TroubleshootingDoc,
            };

            hints.Add(h);
            lines.Add(h.Message);
        }

        if (healthLiveStatus == 200 && healthReadyStatus != 200)
        {
            SupportBundleNextStepHint h = new()
            {
                Id = "health-ready-not-ready",
                Severity = "action",
                Message =
                    $"Liveness is OK but readiness returned HTTP {healthReadyStatus}. Inspect health.json (ready + combined) for dependency failures.",
                DocReference = TroubleshootingDoc,
            };

            hints.Add(h);
            lines.Add(h.Message);
        }

        if (healthLiveStatus == 200 && healthReadyStatus == 200 && healthCombinedStatus != 200)
        {
            SupportBundleNextStepHint h = new()
            {
                Id = "health-combined-degraded",
                Severity = "warning",
                Message =
                    $"GET /health returned HTTP {healthCombinedStatus}. Use the combined body in health.json together with server logs (correlation ID).",
                DocReference = TroubleshootingDoc,
            };

            hints.Add(h);
            lines.Add(h.Message);
        }

        if (openApiHttpStatus != 200)
        {
            SupportBundleNextStepHint h = new()
            {
                Id = "openapi-probe-failed",
                Severity = "warning",
                Message =
                    $"GET /openapi/v1.json returned HTTP {openApiHttpStatus}. The contract endpoint may be down, blocked by auth, or routed incorrectly.",
                DocReference = TroubleshootingDoc,
            };

            hints.Add(h);
            lines.Add(h.Message);
        }

        if (!string.IsNullOrWhiteSpace(apiVersionProbeError))
        {
            SupportBundleNextStepHint h = new()
            {
                Id = "version-probe-failed",
                Severity = "warning",
                Message = "GET /version failed or returned an unusable body — " + apiVersionProbeError,
                DocReference = TroubleshootingDoc,
            };

            hints.Add(h);
            lines.Add(h.Message);
        }

        if (!archlucidJsonPresent)
        {
            SupportBundleNextStepHint h = new()
            {
                Id = "archlucid-json-missing",
                Severity = "info",
                Message =
                    "archlucid.json was not found in the CLI working directory — run the CLI from your project root or create archlucid.json so API URL resolution matches your intent.",
                DocReference = OperatorQuickstartDoc,
            };

            hints.Add(h);
            lines.Add(h.Message);
        }

        if (hasLocalLogExcerpt)
        {
            lines.Add("localLogExcerpt in logs.json may contain recent CLI errors — review before opening a ticket.");
        }

        if (hints.Count == 0)
        {
            lines.Add(
                "Core HTTP probes in this bundle reported success — if symptoms persist, use health.json bodies and server-side logs.");
        }

        lines.Add("Include X-Correlation-ID / correlationId from API errors with server-side logs when you escalate.");
        lines.Add($"Review all JSON for sensitive context; see {PendingQuestionsDoc} before external forwarding.");

        return new SupportBundleNextStepsDocument
        {
            Source = "cli", GeneratedUtc = generatedAtUtcIso, SummaryLines = lines, Hints = hints,
        };
    }
}
