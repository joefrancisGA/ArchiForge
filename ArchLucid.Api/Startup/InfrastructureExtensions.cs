using ArchLucid.Host.Core.Startup;

using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Startup;

internal static class InfrastructureExtensions
{
    /// <summary>
    /// Registers ArchLucid authorization policies (see <see cref="ArchLucidAuthorizationPoliciesExtensions.AddArchLucidAuthorizationPolicies"/>).
    /// </summary>
    /// <remarks>
    /// Fallback policy requires an authenticated principal; use <c>[AllowAnonymous]</c> only for intentional public surface
    /// (e.g. <c>/version</c>, <c>/health/live</c>, <c>/health/ready</c>).
    /// </remarks>
    public static IServiceCollection AddArchLucidAuthorization(this IServiceCollection services)
    {
        return services.AddArchLucidAuthorizationPolicies();
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

    /// <summary>
    /// Registers CORS policy <c>ArchLucid</c>. When <c>Cors:AllowedOrigins</c> is empty, no browser origin is allowed.
    /// Methods and headers are explicit by default; override via <c>Cors:AllowedMethods</c> and <c>Cors:AllowedHeaders</c>.
    /// </summary>
    public static IServiceCollection AddArchLucidCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string[] defaultMethods = ["GET", "POST", "PUT", "DELETE", "OPTIONS"];
        string[] defaultHeaders =
        [
            "Content-Type",
            "Authorization",
            "X-Api-Key",
            "X-Correlation-ID",
            "Idempotency-Key",
            "Accept",
        ];

        services.AddCors(options =>
        {
            options.AddPolicy("ArchLucid", policy =>
            {
                string[] origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

                if (origins.Length == 0)
                {
                    policy.SetIsOriginAllowed(_ => false);
                    return;
                }

                string[]? configuredMethods = configuration.GetSection("Cors:AllowedMethods").Get<string[]>();
                string[] methods = configuredMethods is { Length: > 0 }
                    ? configuredMethods
                    : defaultMethods;

                string[]? configuredHeaders = configuration.GetSection("Cors:AllowedHeaders").Get<string[]>();
                string[] headers = configuredHeaders is { Length: > 0 }
                    ? configuredHeaders
                    : defaultHeaders;

                _ = policy.WithOrigins(origins)
                    .WithMethods(methods)
                    .WithHeaders(headers)
                    .WithExposedHeaders("traceparent", "X-Trace-Id", "X-Correlation-ID");
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
