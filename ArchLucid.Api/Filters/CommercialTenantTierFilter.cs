using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ArchLucid.Api.Filters;

/// <summary>
///     Enforces a minimum <see cref="TenantTier" /> for the current scope (loaded from <c>dbo.Tenants</c>).
///     Returns <c>404 Not Found</c> for Enterprise-only entitlement gates (enumeration suppression); <c>403 Forbidden</c>
///     with Problem Details when the minimum tier is Standard (tenant-visible commercial capabilities).
/// </summary>
public sealed class CommercialTenantTierFilter(
    TenantTier minimumTier,
    ITenantRepository tenantRepository,
    IScopeContextProvider scopeContextProvider) : IAsyncActionFilter
{
    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

    private readonly ITenantRepository _tenantRepository =
        tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated is not true)
        {
            await next();

            return;
        }

        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        TenantRecord? tenant = await _tenantRepository.GetByIdAsync(scope.TenantId, context.HttpContext.RequestAborted);

        if (tenant is null)
        {
            Microsoft.AspNetCore.Mvc.ProblemDetails problem = new()
            {
                Type = ProblemTypes.ResourceNotFound,
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = "The requested resource was not found.",
                Instance = context.HttpContext.Request.Path.Value
            };

            ProblemErrorCodes.AttachErrorCode(problem, problem.Type);
            ProblemSupportHints.AttachForProblemType(problem);
            ProblemCorrelation.Attach(problem, context.HttpContext);
            context.Result = new ObjectResult(problem)
            {
                StatusCode = problem.Status, ContentTypes = { ApplicationProblemMapper.ProblemJsonMediaType }
            };

            return;
        }

        if ((int)tenant.Tier < (int)minimumTier)
        {
            string? instancePath = context.HttpContext.Request.Path.Value;

            context.Result =
                MinimumTierDeniedShouldObfuscate(minimumTier)
                    ? PackagingTierProblemDetailsFactory.CreateObfuscatedNotFound(context.HttpContext, instancePath)
                    : PackagingTierProblemDetailsFactory.CreateTenantProductInsufficientTier(
                        context.HttpContext,
                        minimumTier,
                        instancePath);

            return;
        }

        await next();
    }

    private static bool MinimumTierDeniedShouldObfuscate(TenantTier minimumTier) =>
        minimumTier == TenantTier.Enterprise;
}
