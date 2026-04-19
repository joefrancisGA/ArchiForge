using ArchLucid.Core.Metering;
using ArchLucid.Core.Scoping;

using Microsoft.Extensions.Options;

namespace ArchLucid.Api.Middleware;

/// <summary>Records one <see cref="UsageMeterKind.ApiRequest"/> per completed versioned API call when metering is enabled.</summary>
/// <remarks>
/// Implements <see cref="IMiddleware"/> so <see cref="IUsageMeteringService"/> (scoped) is resolved per request, not at pipeline build time.
/// </remarks>
public sealed class ApiRequestMeteringMiddleware(
    IScopeContextProvider scopeProvider,
    IUsageMeteringService usageMetering,
    IOptionsMonitor<MeteringOptions> meteringOptions,
    ILogger<ApiRequestMeteringMiddleware> logger) : IMiddleware
{
    private readonly IScopeContextProvider _scopeProvider =
        scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));

    private readonly IUsageMeteringService _usageMetering =
        usageMetering ?? throw new ArgumentNullException(nameof(usageMetering));

    private readonly IOptionsMonitor<MeteringOptions> _meteringOptions =
        meteringOptions ?? throw new ArgumentNullException(nameof(meteringOptions));

    private readonly ILogger<ApiRequestMeteringMiddleware> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        await next(context);

        if (!_meteringOptions.CurrentValue.Enabled)
            return;

        string path = context.Request.Path.Value ?? string.Empty;

        if (!path.StartsWith("/v", StringComparison.OrdinalIgnoreCase))
            return;

        if (path.Contains("/health", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/swagger", StringComparison.OrdinalIgnoreCase))
            return;

        ScopeContext scope = _scopeProvider.GetCurrentScope();

        if (scope.TenantId == Guid.Empty)
            return;

        try
        {
            await _usageMetering
                .RecordAsync(
                    new UsageEvent
                    {
                        TenantId = scope.TenantId,
                        WorkspaceId = scope.WorkspaceId,
                        ProjectId = scope.ProjectId,
                        Kind = UsageMeterKind.ApiRequest,
                        Quantity = 1,
                        RecordedUtc = DateTimeOffset.UtcNow,
                        CorrelationId = context.TraceIdentifier,
                    },
                    context.RequestAborted)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(ex, "Usage metering failed for API request (tenant {TenantId}).", scope.TenantId);
            }
        }
    }
}
