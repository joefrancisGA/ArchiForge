using System.Security.Claims;
using System.Threading.RateLimiting;

using ArchLucid.Core.Authorization;
using ArchLucid.Core.Configuration;

using Microsoft.Extensions.Options;

namespace ArchLucid.Api.Startup;

/// <summary>
///     Builds per-request fixed-window partitions keyed by policy, role multiplier segment, then either an authenticated
///     <c>tenant_id</c> claim (when present) or client IP.
/// </summary>
internal static class RateLimitingRolePartitionBuilder
{
    private const string TenantIdClaimType = "tenant_id";

    internal static RateLimitPartition<string> CreateFixedWindow(
        HttpContext httpContext,
        int basePermitLimit,
        int windowMinutes,
        int queueLimit,
        string policyTag)
    {
        RateLimitingRoleMultiplierOptions multOpts = httpContext.RequestServices
                                                         .GetService<IOptions<RateLimitingRoleMultiplierOptions>>()
                                                         ?.Value
                                                     ?? new RateLimitingRoleMultiplierOptions();

        string roleSeg = ResolveRoleSegment(httpContext);
        double mult = ClampMult(MultiplierForSegment(roleSeg, multOpts));
        int permits = Math.Max(1, (int)Math.Round(basePermitLimit * mult));
        string clientKey = ResolveClientPartitionKey(httpContext);
        string partitionKey = $"{policyTag}:{roleSeg}:{clientKey}";
        TimeSpan window = TimeSpan.FromMinutes(windowMinutes);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = window, QueueLimit = queueLimit });
    }

    /// <summary>
    ///     Buckets authenticated callers by <c>tenant_id</c> JWT claim when available; anonymous callers bucket by remote IP.
    /// </summary>
    private static string ResolveClientPartitionKey(HttpContext httpContext)
    {
        ClaimsPrincipal user = httpContext.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            string? tenantRaw = user.FindFirst(TenantIdClaimType)?.Value;

            if (!string.IsNullOrWhiteSpace(tenantRaw) && Guid.TryParse(tenantRaw, out Guid tenantId))
                return $"t:{tenantId:N}";
        }

        string ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return $"ip:{ip}";
    }

    private static string ResolveRoleSegment(HttpContext http)
    {
        ClaimsPrincipal user = http.User;

        if (user.Identity?.IsAuthenticated != true)
            return "anon";

        if (user.IsInRole(ArchLucidRoles.Admin))
            return "admin";

        return user.IsInRole(ArchLucidRoles.Operator) ? "operator" : "reader";
    }

    private static double MultiplierForSegment(string segment, RateLimitingRoleMultiplierOptions o)
    {
        return segment switch
        {
            "admin" => o.Admin,
            "operator" => o.Operator,
            "anon" => o.Anonymous,
            _ => o.Reader
        };
    }

    private static double ClampMult(double value)
    {
        return Math.Clamp(value, 0.25, 10.0);
    }
}
