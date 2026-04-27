using ArchLucid.Core.Tenancy;

using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.ProblemDetails;

/// <summary>
///     Builds RFC 9457 <c>404 Not Found</c> bodies when a route is gated on <see cref="TenantTier" />, so the API
///     does not disclose that a feature exists (see <c>CommercialTenantTierFilter</c>).
/// </summary>
internal static class PackagingTierProblemDetailsFactory
{
    internal static ObjectResult CreatePaymentRequired(
        HttpContext httpContext,
        TenantTier currentTier,
        TenantTier requiredTier,
        string? instancePath)
    {
        // Tier parameters are required by the call site but intentionally not echoed in the response.
        _ = currentTier;
        _ = requiredTier;

        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new()
        {
            Type = ProblemTypes.ResourceNotFound,
            Title = "Not Found",
            Status = StatusCodes.Status404NotFound,
            Detail = "The requested resource was not found.",
            Instance = string.IsNullOrWhiteSpace(instancePath) ? null : instancePath
        };

        ProblemErrorCodes.AttachErrorCode(problem, problem.Type);
        ProblemSupportHints.AttachForProblemType(problem);
        ProblemCorrelation.Attach(problem, httpContext);

        return new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { ApplicationProblemMapper.ProblemJsonMediaType }
        };
    }
}
