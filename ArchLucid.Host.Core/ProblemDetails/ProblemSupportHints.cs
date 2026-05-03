using ArchLucid.Core.Configuration;

namespace ArchLucid.Host.Core.ProblemDetails;

/// <summary>
/// Optional <c>supportHint</c> on problem+json for operators (no secrets; complements <c>errorCode</c>).
/// </summary>
public static class ProblemSupportHints
{
    /// <summary>
    /// Adds <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails.Extensions"/> <c>supportHint</c> when a known <paramref name="problem"/> <c>Type</c> is mapped.
    /// </summary>
    public static void AttachForProblemType(Microsoft.AspNetCore.Mvc.ProblemDetails problem)
    {
        ArgumentNullException.ThrowIfNull(problem);

        string? type = problem.Type;
        if (string.IsNullOrWhiteSpace(type))
            return;

        string? hint = Resolve(type);
        if (!string.IsNullOrWhiteSpace(hint))

            problem.Extensions["supportHint"] = hint;
    }

    private static string? Resolve(string typeUri)
    {
        if (typeUri == ProblemTypes.RunNotFound)

            return "Confirm the run ID. If you use scope headers (x-tenant-id, x-workspace-id, x-project-id), they must match the run�s scope.";

        if (typeUri == ProblemTypes.ManifestNotFound)
            return "Confirm the manifest ID and scope. The manifest may not exist in this tenant/workspace/project.";

        if (typeUri == ProblemTypes.ResourceNotFound)
            return "Confirm the resource identifier and that your caller is authorized for the correct scope.";

        if (typeUri == ProblemTypes.Conflict)
            return
                "Read the detail for state or idempotency context. You may need a new run, a different idempotency key, or to complete prior steps (execute before commit).";

        if (typeUri is ProblemTypes.ValidationFailed or ProblemTypes.BadRequest or ProblemTypes.RequestBodyRequired)
            return "Correct the request using the detail and validation entries above. Swagger (/swagger) lists required fields for each endpoint.";

        if (typeUri == ProblemTypes.InvalidRunState)
            return "Check run status (GET run detail): execute agent tasks before commit, or avoid repeating a terminal step.";

        if (typeUri == ProblemTypes.CommitFailed)
            return "Review the detail; ensure all tasks have results and the run is in the expected state. See server logs for RunId.";

        if (typeUri == ProblemTypes.AgentResultRequired)
            return "Submit the missing agent result payload, then retry.";

        if (typeUri == ProblemTypes.UnavailableInProduction)
            return "This operation is restricted in the current environment; use Development or an approved configuration.";

        if (typeUri is ProblemTypes.DatabaseTimeout or ProblemTypes.DatabaseUnavailable)
            return "Retry after a short wait. If it persists, verify SQL connectivity, migrations, and GET /health/ready.";

        if (typeUri == ProblemTypes.CircuitBreakerOpen)
            return "Downstream AI calls are paused after repeated failures. Retry later; check Azure OpenAI configuration and quotas if applicable.";

        if (typeUri == ProblemTypes.LlmTokenQuotaExceeded)
            return "Raise LlmTokenQuota limits, wait for the sliding window to elapse, or reduce LLM usage. See docs/OPERATIONS_LLM_QUOTA.md.";

        if (typeUri == ProblemTypes.ComparisonVerificationFailed)
            return "Review drift fields in the response. Regenerate or verify replay inputs against stored artifacts if you need a passing comparison.";

        if (typeUri == ProblemTypes.BatchReplayAllFailed)
            return
                "Every comparisonRecordId in the batch failed to replay. Fix IDs or replay parameters (or inspect API logs with correlation ID); successful batches include batch-replay-manifest.json with per-id errors when some fail.";

        if (typeUri == ProblemTypes.PolicyPackVersionNotFound)
            return "Confirm the policy pack version exists and is deployed to the environment; check governance configuration.";

        if (typeUri is ProblemTypes.ExportFailed or ProblemTypes.DeterminismFailed)
            return "Retry once. If it persists, capture correlation ID and check API logs for the same RunId or export id.";

        if (typeUri == ProblemTypes.GraphTooLargeForFullResponse)
            return
                "Use GET /v1/graph/runs/{runId}/nodes with page/pageSize to retrieve the architecture graph in pages (max page size 200). Cross-page edges are omitted per page; export or downstream analytics may be needed for full linkage.";

        if (typeUri == ProblemTypes.RequestPayloadTooLarge)
            return
                $"Shrink the POST /v1/architecture/request JSON (documents, IaC payloads, hints) or raise {ArchitectureRunCreationPayloadLimitsOptions.MaxPayloadBytesKey} with operator approval (HTTP 413 uses Content-Length; legacy ArchLucid:ContextIngestion:MaxPayloadBytes is still forwarded when the new key is unset).";

        if (typeUri == ProblemTypes.TrialExpired)
            return "Convert the tenant trial (POST /v1/tenant/convert) or purchase a subscription to lift trial limits; see docs/security/TRIAL_LIMITS.md.";

        if (typeUri == ProblemTypes.PackagingTierInsufficient)
            return
                "This route requires a higher commercial tenant tier. Use extension fields pricingUrl/upgradeUrl, POST /v1/tenant/billing/checkout, or your sales order path.";

        return typeUri == ProblemTypes.InternalError
            ? "Retry once. If it persists, provide traceId (and X-Correlation-ID if available) to support; do not paste secrets."
            : null;
    }
}
