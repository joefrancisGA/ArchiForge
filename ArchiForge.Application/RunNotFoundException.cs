namespace ArchiForge.Application;

/// <summary>
/// Thrown when an architecture run ID does not exist. Maps to HTTP 404 with problem type <c>run-not-found</c>.
/// </summary>
public sealed class RunNotFoundException(string runId) : Exception($"Run '{runId}' was not found.")
{
    public string RunId { get; } = runId;
}
