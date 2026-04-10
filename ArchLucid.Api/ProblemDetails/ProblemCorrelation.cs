using Microsoft.AspNetCore.Http;

namespace ArchLucid.Api.ProblemDetails;

/// <summary>
/// Adds <c>correlationId</c> to problem JSON (MVC <c>ProblemDetails.Extensions</c>, promoted to root in JSON), matching
/// <c>X-Correlation-ID</c> on the response so operators can triage when headers are stripped or logs are JSON-only.
/// </summary>
public static class ProblemCorrelation
{
    /// <summary>Serialized at the root of <c>application/problem+json</c> (extension keys are promoted).</summary>
    public const string ExtensionKey = "correlationId";

    /// <summary>
    /// Sets <see cref="ExtensionKey"/> from <see cref="HttpContext.TraceIdentifier"/> when non-empty
    /// (after <see cref="ArchLucid.Host.Core.Middleware.CorrelationIdMiddleware"/> runs).
    /// </summary>
    public static void Attach(Microsoft.AspNetCore.Mvc.ProblemDetails problem, HttpContext? httpContext)
    {
        ArgumentNullException.ThrowIfNull(problem);

        if (httpContext is null)
            return;

        string id = httpContext.TraceIdentifier;

        if (string.IsNullOrWhiteSpace(id))
            return;

        problem.Extensions[ExtensionKey] = id;
    }
}
