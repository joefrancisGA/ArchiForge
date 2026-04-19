using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Tenancy;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;
using ArchLucid.Host.Core.Authorization;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace ArchLucid.Api.Filters;

/// <summary>
/// Authorization handler enforcing <see cref="TrialActiveRequirement"/> via <see cref="TrialLimitGate"/> (HTTP-only wiring;
/// worker paths rely on <see cref="ArchLucid.Core.Tenancy.ITenantRepository.TryIncrementActiveTrialRunAsync"/> throwing).
/// </summary>
public sealed class TrialLimitAuthorizationHandler : AuthorizationHandler<TrialActiveRequirement>
{
    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TrialActiveRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
        {
            context.Fail();

            return;
        }

        Endpoint? endpoint = httpContext.GetEndpoint();

        if (endpoint?.Metadata.GetMetadata<SkipTrialWriteLimitAttribute>() is not null)
        {
            context.Succeed(requirement);

            return;
        }

        TrialLimitGate gate = httpContext.RequestServices.GetRequiredService<TrialLimitGate>();
        IScopeContextProvider scopes = httpContext.RequestServices.GetRequiredService<IScopeContextProvider>();
        ScopeContext scope = scopes.GetCurrentScope();

        try
        {
            if (string.Equals(httpContext.Request.Method, HttpMethods.Delete, StringComparison.OrdinalIgnoreCase))
            {
                await gate.GuardDeleteAsync(scope, httpContext.RequestAborted);
            }
            else
            {
                await gate.GuardWriteAsync(scope, httpContext.RequestAborted);
            }

            context.Succeed(requirement);
        }
        catch (TrialLimitExceededException ex)
        {
            httpContext.Items["ArchLucid.TrialLimitExceeded"] = ex;
            context.Fail();
        }
    }
}

/// <summary>Turns failed trial authorization into <c>402 Payment Required</c> + problem+json (instead of 403).</summary>
public sealed class TrialLimitAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    /// <inheritdoc />
    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded)
        {
            await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);

            return;
        }

        if (context.Items.TryGetValue("ArchLucid.TrialLimitExceeded", out object? o) &&
            o is TrialLimitExceededException ex)
        {
            await TrialLimitProblemResponse.WriteResponseAsync(context, ex);

            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
