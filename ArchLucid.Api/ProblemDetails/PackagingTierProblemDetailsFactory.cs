using ArchLucid.Core.Tenancy;

using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.ProblemDetails;

/// <summary>
///     Builds RFC 9457 tier-gate bodies for commercial packaging: Enterprise-only probes use obfuscated **404**, Standard
///     capabilities use **403** with <see cref="ProblemTypes.PackagingTierInsufficient" /> so operators can correlate
///     entitlement gaps (<see cref="CommercialTenantTierFilter" />).
/// </summary>
internal static class PackagingTierProblemDetailsFactory
{
    /// <summary>
    ///     Returns <see cref="StatusCodes.Status404NotFound"/> so callers cannot infer hidden admin / entitlement-gated capabilities.
    /// </summary>
    internal static ObjectResult CreateObfuscatedNotFound(HttpContext httpContext, string? instancePath)
    {
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

    /// <summary>
    ///     Returns <see cref="StatusCodes.Status403Forbidden"/> with Problem Details for tenant-visible routes so operators can correlate denials without disclosing gated admin URLs.
    /// </summary>
    internal static ObjectResult CreateTenantProductInsufficientTier(
        HttpContext httpContext,
        TenantTier requiredTier,
        string? instancePath)
    {
        _ = requiredTier;

        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new()
        {
            Type = ProblemTypes.PackagingTierInsufficient,
            Title = "Insufficient commercial entitlement",
            Status = StatusCodes.Status403Forbidden,
            Detail =
                "The authenticated tenant must be upgraded before this capability can be used. Contact sales or billing for the appropriate operate tier.",
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
