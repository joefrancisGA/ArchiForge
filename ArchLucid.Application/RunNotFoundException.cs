namespace ArchLucid.Application;
/// <summary>
///     Thrown when an architecture run ID does not exist. Maps to HTTP 404 with problem type <c>run-not-found</c>.
/// </summary>
public sealed class RunNotFoundException(string runId) : Exception($"Run '{runId}' was not found.")
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(runId);
    private static byte __ValidatePrimaryConstructorArguments(System.String runId)
    {
        ArgumentNullException.ThrowIfNull(runId);
        return (byte)0;
    }

    public string RunId { get; } = runId;
}