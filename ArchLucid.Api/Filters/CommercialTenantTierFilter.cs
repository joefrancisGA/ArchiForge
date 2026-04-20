using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ArchLucid.Api.Filters;

/// <summary>
/// Enforces a minimum <see cref="TenantTier"/> for the current scope (loaded from <c>dbo.Tenants</c>).
/// Returns <c>402 Payment Required</c> when the tenant is below the required tier (product decision: Stripe-style code).
/// </summary>
public sealed class CommercialTenantTierFilter(
    TenantTier minimumTier,
    ITenantRepository tenantRepository,
    IScopeContextProvider scopeContextProvider) : IAsyncActionFilter
{
    private readonly TenantTier _minimumTier = minimumTier;
    private readonly ITenantRepository _tenantRepository =
        tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.User?.Identity?.IsAuthenticated is not true)
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
                Title = "Forbidden",
                Status = StatusCodes.Status403Forbidden,
                Detail = "Tenant record was not found for the current scope.",
                Instance = context.HttpContext.Request.Path.Value,
            };

            ProblemErrorCodes.AttachErrorCode(problem, problem.Type);
            ProblemSupportHints.AttachForProblemType(problem);
            ProblemCorrelation.Attach(problem, context.HttpContext);
            context.Result = new ObjectResult(problem)
            {
                StatusCode = problem.Status,
                ContentTypes = { ApplicationProblemMapper.ProblemJsonMediaType },
            };

            return;
        }

        if ((int)tenant.Tier < (int)_minimumTier)
        {
            context.Result = PackagingTierProblemDetailsFactory.CreatePaymentRequired(
                context.HttpContext,
                tenant.Tier,
                _minimumTier,
                context.HttpContext.Request.Path.Value);

            return;
        }

        await next();
    }
}
