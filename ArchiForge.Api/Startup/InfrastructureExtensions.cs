using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Startup;

internal static class InfrastructureExtensions
{
    public static IServiceCollection AddArchiForgeAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
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

    public static IServiceCollection AddArchiForgeRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            var fixedPermitLimit = configuration.GetValue("RateLimiting:FixedWindow:PermitLimit", 100);
            var fixedWindowMinutes = configuration.GetValue("RateLimiting:FixedWindow:WindowMinutes", 1);
            var fixedQueueLimit = configuration.GetValue("RateLimiting:FixedWindow:QueueLimit", 0);

            options.AddFixedWindowLimiter("fixed", config =>
            {
                config.Window = TimeSpan.FromMinutes(fixedWindowMinutes);
                config.PermitLimit = fixedPermitLimit;
                config.QueueLimit = fixedQueueLimit;
            });

            var expensivePermitLimit = configuration.GetValue("RateLimiting:Expensive:PermitLimit", 20);
            var expensiveWindowMinutes = configuration.GetValue("RateLimiting:Expensive:WindowMinutes", 1);
            var expensiveQueueLimit = configuration.GetValue("RateLimiting:Expensive:QueueLimit", 0);

            options.AddFixedWindowLimiter("expensive", config =>
            {
                config.Window = TimeSpan.FromMinutes(expensiveWindowMinutes);
                config.PermitLimit = expensivePermitLimit;
                config.QueueLimit = expensiveQueueLimit;
            });

            var replayLightPermitLimit = configuration.GetValue("RateLimiting:Replay:Light:PermitLimit", 60);
            var replayLightWindowMinutes = configuration.GetValue("RateLimiting:Replay:Light:WindowMinutes", 1);
            var replayHeavyPermitLimit = configuration.GetValue("RateLimiting:Replay:Heavy:PermitLimit", 15);
            var replayHeavyWindowMinutes = configuration.GetValue("RateLimiting:Replay:Heavy:WindowMinutes", 1);

            options.AddPolicy("replay", httpContext =>
            {
                var fmt = (httpContext.Request.Query["format"].ToString()).Trim().ToLowerInvariant();
                var isHeavy = fmt is "docx" or "pdf";
                var window = TimeSpan.FromMinutes(isHeavy ? replayHeavyWindowMinutes : replayLightWindowMinutes);
                var permits = isHeavy ? replayHeavyPermitLimit : replayLightPermitLimit;

                var user = httpContext.User.Identity?.Name;
                var key = string.IsNullOrWhiteSpace(user)
                    ? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous"
                    : user;

                var partitionKey = $"{key}:{(isHeavy ? "heavy" : "light")}";
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

    public static IServiceCollection AddArchiForgeCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("ArchiForge", policy =>
            {
                var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
                if (origins.Length > 0)
                {
                    policy.WithOrigins(origins)
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                }
                else
                {
                    policy.SetIsOriginAllowed(_ => false);
                }
            });
        });
        return services;
    }
}
