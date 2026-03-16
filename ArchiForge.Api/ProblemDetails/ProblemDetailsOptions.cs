namespace ArchiForge.Api.ProblemDetails;

/// <summary>
/// Well-known problem type URIs for RFC 7807 (relative to the base).
/// </summary>
public static class ProblemTypes
{
    public const string Base = "https://archiforge.example.org/errors";

    public const string RequestBodyRequired = Base + "#request-body-required";
    public const string ValidationFailed = Base + "#validation-failed";
    public const string RunNotFound = Base + "#run-not-found";
    public const string ManifestNotFound = Base + "#manifest-not-found";
    public const string AgentResultRequired = Base + "#agent-result-required";
    public const string CommitFailed = Base + "#commit-failed";
    public const string UnavailableInProduction = Base + "#unavailable-in-production";
    public const string InternalError = Base + "#internal-error";
    public const string BadRequest = Base + "#bad-request";
    public const string ResourceNotFound = Base + "#resource-not-found";

    public const string InvalidRunState = Base + "#invalid-run-state";
    public const string DeterminismFailed = Base + "#determinism-failed";
    public const string ExportFailed = Base + "#export-failed";
}
