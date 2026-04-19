using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Tenancy;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;

namespace ArchLucid.Api.Middleware;

/// <summary>
/// Reserves a trial seat for the authenticated principal before authorization so seat exhaustion surfaces as
/// <c>402 Payment Required</c> with the same problem contract as other trial blocks.
/// </summary>
public sealed class TrialSeatReservationMiddleware(RequestDelegate next)
{
    private static bool SkipSeatAccounting(PathString path)
    {
        if (path == "/" || path.StartsWithSegments("/robots.txt", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/sitemap.xml", StringComparison.OrdinalIgnoreCase))

            return true;


        return path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/version", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/scalar", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/v1/register", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Invokes seat reservation then the rest of the pipeline.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (SkipSeatAccounting(context.Request.Path))
        {
            await next(context);

            return;
        }

        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await next(context);

            return;
        }

        string? principalKey =
            context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

        if (string.IsNullOrWhiteSpace(principalKey))
        {
            await next(context);

            return;
        }

        TrialSeatAccountant accountant = context.RequestServices.GetRequiredService<TrialSeatAccountant>();
        IScopeContextProvider scopes = context.RequestServices.GetRequiredService<IScopeContextProvider>();

        try
        {
            await accountant.TryReserveSeatAsync(scopes.GetCurrentScope(), principalKey, context.RequestAborted);
        }
        catch (TrialLimitExceededException ex)
        {
            await TrialLimitProblemResponse.WriteResponseAsync(context, ex);

            return;
        }

        await next(context);
    }
}
