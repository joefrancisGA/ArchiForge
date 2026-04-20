using ArchLucid.Core.Tenancy;

using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.ProblemDetails;

/// <summary>Builds RFC 9457 <c>402 Payment Required</c> bodies when a route is gated on <see cref="TenantTier"/>.</summary>
internal static class PackagingTierProblemDetailsFactory
{
    internal static ObjectResult CreatePaymentRequired(
        HttpContext httpContext,
        TenantTier currentTier,
        TenantTier requiredTier,
        string? instancePath)
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new()
        {
            Type = ProblemTypes.PackagingTierInsufficient,
            Title = "Payment required",
            Status = StatusCodes.Status402PaymentRequired,
            Detail =
                $"This capability requires commercial tier {requiredTier} or higher. The current tenant tier is {currentTier}. Upgrade the workspace (see docs/go-to-market/PRICING_PHILOSOPHY.md) or use billing checkout.",
            Instance = string.IsNullOrWhiteSpace(instancePath) ? null : instancePath,
        };

        ProblemErrorCodes.AttachErrorCode(problem, problem.Type);
        ProblemSupportHints.AttachForProblemType(problem);
        ProblemCorrelation.Attach(problem, httpContext);
        problem.Extensions["currentTier"] = currentTier.ToString();
        problem.Extensions["requiredTier"] = requiredTier.ToString();

        return new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { ApplicationProblemMapper.ProblemJsonMediaType },
        };
    }
}
