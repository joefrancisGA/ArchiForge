using System.Security.Claims;
using System.Threading.RateLimiting;

using ArchLucid.Core.Authorization;
using ArchLucid.Core.Configuration;

using Microsoft.Extensions.Options;

namespace ArchLucid.Api.Startup;

/// <summary>
/// Builds per-request fixed-window partitions keyed by rate-limit policy, resolved role segment, and client IP so
/// multipliers apply without sharing one global bucket across roles.
/// </summary>
internal static class RateLimitingRolePartitionBuilder
{
    internal static RateLimitPartition<string> CreateFixedWindow(
        HttpContext httpContext,
        int basePermitLimit,
        int windowMinutes,
        int queueLimit,
        string policyTag)
    {
        RateLimitingRoleMultiplierOptions multOpts = httpContext.RequestServices
                                                         .GetService<IOptions<RateLimitingRoleMultiplierOptions>>()?.Value
                                                     ?? new RateLimitingRoleMultiplierOptions();

        string roleSeg = ResolveRoleSegment(httpContext);
        double mult = ClampMult(MultiplierForSegment(roleSeg, multOpts));
        int permits = Math.Max(1, (int)Math.Round(basePermitLimit * mult));
        string ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        string partitionKey = $"{policyTag}:{roleSeg}:{ip}";
        TimeSpan window = TimeSpan.FromMinutes(windowMinutes);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permits,
                Window = window,
                QueueLimit = queueLimit
            });
    }

    private static string ResolveRoleSegment(HttpContext http)
    {
        ClaimsPrincipal? user = http.User;

        if (user?.Identity?.IsAuthenticated != true)
            return "anon";


        if (user.IsInRole(ArchLucidRoles.Admin))
            return "admin";


        if (user.IsInRole(ArchLucidRoles.Operator))
            return "operator";


        return "reader";
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
