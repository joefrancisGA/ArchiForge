namespace ArchLucid.Host.Core.ProblemDetails;

/// <summary>
/// Well-known problem type URIs for RFC 9457 Problem Details (relative to the base; obsoletes RFC 7807).
/// </summary>
public static class ProblemTypes
{
    public const string Base = "https://archlucid.example.org/errors";

    public const string RequestBodyRequired = Base + "#request-body-required";
    public const string ValidationFailed = Base + "#validation-failed";
    public const string RunNotFound = Base + "#run-not-found";
    public const string ManifestNotFound = Base + "#manifest-not-found";
    public const string AgentResultRequired = Base + "#agent-result-required";
    public const string CommitFailed = Base + "#commit-failed";
    public const string UnavailableInProduction = Base + "#unavailable-in-production";
    public const string InternalError = Base + "#internal-error";
    public const string BadRequest = Base + "#bad-request";

    /// <summary>Reviewer attempted to approve or reject their own governance request (segregation of duties).</summary>
    public const string GovernanceSelfApproval = Base + "#governance-self-approval";

    /// <summary>Optional pre-commit governance blocked manifest commit (Critical findings under enforcing policy assignment).</summary>
    public const string GovernancePreCommitBlocked = Base + "#governance-pre-commit-blocked";

    public const string ResourceNotFound = Base + "#resource-not-found";

    /// <summary>59R learning plan not found for the current scope.</summary>
    public const string LearningImprovementPlanNotFound = Base + "#learning-improvement-plan-not-found";

    /// <summary>60R evolution candidate change set not found for the current scope.</summary>
    public const string EvolutionCandidateChangeSetNotFound = Base + "#evolution-candidate-change-set-not-found";

    public const string InvalidRunState = Base + "#invalid-run-state";
    public const string DeterminismFailed = Base + "#determinism-failed";
    public const string ExportFailed = Base + "#export-failed";

    /// <summary>Comparison replay verification did not pass (semantic mismatch).</summary>
    public const string ComparisonVerificationFailed = Base + "#comparison-verification-failed";

    /// <summary>Request cannot be applied due to a resource state conflict.</summary>
    public const string Conflict = Base + "#conflict";

    public const string PolicyPackVersionNotFound = Base + "#policy-pack-version-not-found";

    /// <summary>A SQL or database timeout occurred; the request may succeed on retry.</summary>
    public const string DatabaseTimeout = Base + "#database-timeout";

    /// <summary>The database is unreachable or returned a transient error.</summary>
    public const string DatabaseUnavailable = Base + "#database-unavailable";

    /// <summary>Azure OpenAI (or embedding) calls are blocked by the circuit breaker after repeated failures.</summary>
    public const string CircuitBreakerOpen = Base + "#circuit-breaker-open";

    /// <summary>Tenant exceeded configured LLM token quota for the current sliding window.</summary>
    public const string LlmTokenQuotaExceeded = Base + "#llm-token-quota-exceeded";

    /// <summary>Batch comparison replay had no successful replays for any requested record ID.</summary>
    public const string BatchReplayAllFailed = Base + "#batch-replay-all-failed";

    /// <summary>Tenant self-service trial expired or trial quota (runs/seats) exhausted; mutating authority operations are blocked.</summary>
    public const string TrialExpired = "https://archlucid.dev/problem/trial-expired";

    /// <summary>Caller is authenticated but the tenant commercial tier is below the capability required for this route (HTTP 402).</summary>
    public const string PackagingTierInsufficient = Base + "#packaging-tier-insufficient";
}
