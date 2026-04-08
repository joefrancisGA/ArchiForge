using ArchLucid.Api.Auth.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Startup;

internal static class InfrastructureExtensions
{
    /// <summary>
    /// Single registration point for all ArchLucid authorization policies:
    /// role-based (ReadAuthority, ExecuteAuthority, AdminAuthority) and
    /// claim-based (CanCommitRuns, CanSeedResults, etc.).
    /// </summary>
    /// <remarks>
    /// <see cref="AuthorizationOptions.FallbackPolicy"/> requires an authenticated principal so new controllers
    /// are closed by default; use <c>[AllowAnonymous]</c> only for intentional public surface (e.g. <c>/version</c>, <c>/health/live</c>, <c>/health/ready</c>).
    /// </remarks>
    public static IServiceCollection AddArchLucidAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(
                new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build())
            .AddPolicy(ArchLucidPolicies.ReadAuthority, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(
                    ArchLucidRoles.Reader,
                    ArchLucidRoles.Operator,
                    ArchLucidRoles.Admin);
            })
            .AddPolicy(ArchLucidPolicies.ExecuteAuthority, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(
                    ArchLucidRoles.Operator,
                    ArchLucidRoles.Admin);
            })
            .AddPolicy(ArchLucidPolicies.AdminAuthority, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(ArchLucidRoles.Admin);
            })
            .AddPolicy("CanCommitRuns", policy =>
                policy.RequireClaim("permission", "commit:run"))
            .AddPolicy("CanSeedResults", policy =>
                policy.RequireClaim("permission", "seed:results"))
            .AddPolicy("CanExportConsultingDocx", policy =>
                policy.RequireClaim("permission", "export:consulting-docx"))
            .AddPolicy("CanReplayComparisons", policy =>
                policy.RequireClaim("permission", "replay:comparisons"))
            .AddPolicy("CanViewReplayDiagnostics", policy =>
                policy.RequireClaim("permission", "replay:diagnostics"));
        return services;
    }

    public static IServiceCollection AddArchLucidRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            int fixedPermitLimit = configuration.GetValue("RateLimiting:FixedWindow:PermitLimit", 100);
            int fixedWindowMinutes = configuration.GetValue("RateLimiting:FixedWindow:WindowMinutes", 1);
            int fixedQueueLimit = configuration.GetValue("RateLimiting:FixedWindow:QueueLimit", 0);

            options.AddFixedWindowLimiter("fixed", config =>
            {
                config.Window = TimeSpan.FromMinutes(fixedWindowMinutes);
                config.PermitLimit = fixedPermitLimit;
                config.QueueLimit = fixedQueueLimit;
            });

            int expensivePermitLimit = configuration.GetValue("RateLimiting:Expensive:PermitLimit", 20);
            int expensiveWindowMinutes = configuration.GetValue("RateLimiting:Expensive:WindowMinutes", 1);
            int expensiveQueueLimit = configuration.GetValue("RateLimiting:Expensive:QueueLimit", 0);

            options.AddFixedWindowLimiter("expensive", config =>
            {
                config.Window = TimeSpan.FromMinutes(expensiveWindowMinutes);
                config.PermitLimit = expensivePermitLimit;
                config.QueueLimit = expensiveQueueLimit;
            });

            int replayLightPermitLimit = configuration.GetValue("RateLimiting:Replay:Light:PermitLimit", 60);
            int replayLightWindowMinutes = configuration.GetValue("RateLimiting:Replay:Light:WindowMinutes", 1);
            int replayHeavyPermitLimit = configuration.GetValue("RateLimiting:Replay:Heavy:PermitLimit", 15);
            int replayHeavyWindowMinutes = configuration.GetValue("RateLimiting:Replay:Heavy:WindowMinutes", 1);

            options.AddPolicy("replay", httpContext =>
            {
                string fmt = httpContext.Request.Query["format"].ToString().Trim().ToLowerInvariant();
                bool isHeavy = fmt is "docx" or "pdf";
                TimeSpan window = TimeSpan.FromMinutes(isHeavy ? replayHeavyWindowMinutes : replayLightWindowMinutes);
                int permits = isHeavy ? replayHeavyPermitLimit : replayLightPermitLimit;

                string? user = httpContext.User.Identity?.Name;
                string key = string.IsNullOrWhiteSpace(user)
                    ? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous"
                    : user;

                string partitionKey = $"{key}:{(isHeavy ? "heavy" : "light")}";
                return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permits,
                        Window = window,
                        QueueLimit = 0
                    });
            });
        });
        return services;
    }

    public static IServiceCollection AddArchLucidCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("ArchLucid", policy =>
            {
                string[] origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
                if (origins.Length > 0)
                
                    policy.WithOrigins(origins)
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                
                else
                
                    policy.SetIsOriginAllowed(_ => false);
                
            });
        });
        return services;
    }

    /// <summary>Enables Brotli/Gzip for HTTPS responses (default MIME types include JSON).</summary>
    public static IServiceCollection AddArchLucidResponseCompression(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
        });
        return services;
    }
}
