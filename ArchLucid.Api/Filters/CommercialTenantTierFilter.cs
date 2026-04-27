using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ArchLucid.Api.Filters;

/// <summary>
///     Enforces a minimum <see cref="TenantTier" /> for the current scope (loaded from <c>dbo.Tenants</c>).
///     Returns <c>404 Not Found</c> when the scope is not entitled so callers cannot infer hidden capabilities.
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
            context.Result = PackagingTierProblemDetailsFactory.CreatePaymentRequired(
                context.HttpContext,
                tenant.Tier,
                minimumTier,
                context.HttpContext.Request.Path.Value);

            return;
        }

        await next();
    }
}
