namespace ArchiForge.Host.Core.ProblemDetails;

/// <summary>
/// Stable machine-readable codes in Problem Details <c>extensions.errorCode</c> for clients and automation.
/// </summary>
public static class ProblemErrorCodes
{
    public const string Unspecified = "UNSPECIFIED";

    public const string InternalError = "INTERNAL_ERROR";

    public const string RequestBodyRequired = "REQUEST_BODY_REQUIRED";

    public const string ValidationFailed = "VALIDATION_FAILED";

    public const string RunNotFound = "RUN_NOT_FOUND";

    public const string ManifestNotFound = "MANIFEST_NOT_FOUND";

    public const string AgentResultRequired = "AGENT_RESULT_REQUIRED";

    public const string CommitFailed = "COMMIT_FAILED";

    public const string UnavailableInProduction = "UNAVAILABLE_IN_PRODUCTION";

    public const string BadRequest = "BAD_REQUEST";

    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";

    public const string InvalidRunState = "INVALID_RUN_STATE";

    public const string DeterminismFailed = "DETERMINISM_FAILED";

    public const string ExportFailed = "EXPORT_FAILED";

    public const string ComparisonVerificationFailed = "COMPARISON_VERIFICATION_FAILED";

    public const string Conflict = "CONFLICT";

    public const string PolicyPackVersionNotFound = "POLICY_PACK_VERSION_NOT_FOUND";

    public const string DatabaseTimeout = "DATABASE_TIMEOUT";

    public const string DatabaseUnavailable = "DATABASE_UNAVAILABLE";

    public const string CircuitBreakerOpen = "CIRCUIT_BREAKER_OPEN";

    public const string LlmTokenQuotaExceeded = "LLM_TOKEN_QUOTA_EXCEEDED";

    public const string BatchReplayAllFailed = "BATCH_REPLAY_ALL_FAILED";

    /// <summary>Maps a <see cref="ProblemTypes"/> URI to <see cref="ProblemErrorCodes"/>; returns <see cref="Unspecified"/> when unknown.</summary>
    public static string ResolveFromProblemType(string? problemTypeUri)
    {
        if (string.IsNullOrWhiteSpace(problemTypeUri))
            return Unspecified;

        if (problemTypeUri == ProblemTypes.RequestBodyRequired)
            return RequestBodyRequired;

        if (problemTypeUri == ProblemTypes.ValidationFailed)
            return ValidationFailed;

        if (problemTypeUri == ProblemTypes.RunNotFound)
            return RunNotFound;

        if (problemTypeUri == ProblemTypes.ManifestNotFound)
            return ManifestNotFound;

        if (problemTypeUri == ProblemTypes.AgentResultRequired)
            return AgentResultRequired;

        if (problemTypeUri == ProblemTypes.CommitFailed)
            return CommitFailed;

        if (problemTypeUri == ProblemTypes.UnavailableInProduction)
            return UnavailableInProduction;

        if (problemTypeUri == ProblemTypes.InternalError)
            return InternalError;

        if (problemTypeUri == ProblemTypes.BadRequest)
            return BadRequest;

        if (problemTypeUri == ProblemTypes.ResourceNotFound)
            return ResourceNotFound;

        if (problemTypeUri == ProblemTypes.InvalidRunState)
            return InvalidRunState;

        if (problemTypeUri == ProblemTypes.DeterminismFailed)
            return DeterminismFailed;

        if (problemTypeUri == ProblemTypes.ExportFailed)
            return ExportFailed;

        if (problemTypeUri == ProblemTypes.ComparisonVerificationFailed)
            return ComparisonVerificationFailed;

        if (problemTypeUri == ProblemTypes.Conflict)
            return Conflict;

        if (problemTypeUri == ProblemTypes.PolicyPackVersionNotFound)
            return PolicyPackVersionNotFound;

        if (problemTypeUri == ProblemTypes.DatabaseTimeout)
            return DatabaseTimeout;

        if (problemTypeUri == ProblemTypes.DatabaseUnavailable)
            return DatabaseUnavailable;

        if (problemTypeUri == ProblemTypes.BatchReplayAllFailed)
            return BatchReplayAllFailed;

        if (problemTypeUri == ProblemTypes.CircuitBreakerOpen)
            return CircuitBreakerOpen;

        return problemTypeUri == ProblemTypes.LlmTokenQuotaExceeded ? LlmTokenQuotaExceeded : Unspecified;
    }

    /// <summary>Attaches <c>extensions.errorCode</c> derived from <paramref name="problemTypeUri"/>.</summary>
    public static void AttachErrorCode(Microsoft.AspNetCore.Mvc.ProblemDetails problem, string? problemTypeUri)
    {
        ArgumentNullException.ThrowIfNull(problem);
        problem.Extensions["errorCode"] = ResolveFromProblemType(problemTypeUri);
    }
}
