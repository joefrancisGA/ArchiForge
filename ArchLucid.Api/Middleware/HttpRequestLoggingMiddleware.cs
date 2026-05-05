using System.Diagnostics;

using ArchLucid.Core.Diagnostics;

using Microsoft.Extensions.Primitives;

namespace ArchLucid.Api.Middleware;

/// <summary>
///     Structured request lifecycle logs: method, path, status code, duration, optional <c>X-Correlation-ID</c>.
/// </summary>
/// <remarks>
///     Does not enumerate request headers or read bodies; never logs Authorization, cookies, or query strings.
///     Register immediately after <see cref="ArchLucid.Host.Core.Middleware.CorrelationIdMiddleware" /> so the
///     correlation header and <see cref="HttpContext.TraceIdentifier" /> are authoritative.
/// </remarks>
public sealed class HttpRequestLoggingMiddleware(RequestDelegate next, ILogger<HttpRequestLoggingMiddleware> logger)
{
    private const string CorrelationHeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly ILogger<HttpRequestLoggingMiddleware> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private static readonly EventId StartedEvent = new(10_701, "Http.Request.Started");

    private static readonly EventId CompletedEvent = new(10_702, "Http.Request.Completed");

    /// <inheritdoc />
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string methodSanitized = LogSanitizer.Sanitize(context.Request.Method ?? string.Empty);

        PathString logicalPath =
            context.Request.PathBase.HasValue ? context.Request.PathBase + context.Request.Path : context.Request.Path;

        string pathSanitized = LogSanitizer.Sanitize(logicalPath.Value ?? string.Empty);

        string? correlationIdSanitized = ResolveCorrelationIdentifierForLogging(context);

        if (_logger.IsEnabled(LogLevel.Information))
            LogStarted(_logger, methodSanitized, pathSanitized, correlationIdSanitized);

        Stopwatch stopwatch = Stopwatch.StartNew();

        return InvokeCoreAsync(context, methodSanitized, pathSanitized, correlationIdSanitized, stopwatch);
    }

    internal static string? ResolveCorrelationIdentifierForLogging(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Response.Headers.TryGetValue(CorrelationHeaderName, out StringValues values))
        {
            string? raw = values.FirstOrDefault();
            string sanitized = LogSanitizer.Sanitize((raw ?? string.Empty).Trim());

            if (!string.IsNullOrEmpty(sanitized))
                return sanitized;
        }

        string fallback = LogSanitizer.Sanitize(context.TraceIdentifier ?? string.Empty);

        if (string.IsNullOrEmpty(fallback))
            return null;

        return fallback;
    }

    private static void LogStarted(
        ILogger<HttpRequestLoggingMiddleware> logger,
        string methodSanitized,
        string pathSanitized,
        string? correlationIdSanitized)
    {
        // codeql[cs/log-forging]: method/path sanitized via LogSanitizer; no raw Authorization or body fields.
        if (correlationIdSanitized is null)
        {
            logger.LogInformation(
                StartedEvent,
                "HTTP request started {HttpMethod} {HttpPath}",
                methodSanitized,
                pathSanitized);

            return;
        }

        logger.LogInformation(
            StartedEvent,
            "HTTP request started {HttpMethod} {HttpPath} {HttpCorrelationId}",
            methodSanitized,
            pathSanitized,
            correlationIdSanitized);
    }

    private async Task InvokeCoreAsync(
        HttpContext context,
        string methodSanitized,
        string pathSanitized,
        string? correlationIdSanitized,
        Stopwatch stopwatch)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        finally
        {
            stopwatch.Stop();

            if (_logger.IsEnabled(LogLevel.Information))
                LogCompleted(
                    _logger,
                    methodSanitized,
                    pathSanitized,
                    correlationIdSanitized,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
        }
    }

    private static void LogCompleted(
        ILogger<HttpRequestLoggingMiddleware> logger,
        string methodSanitized,
        string pathSanitized,
        string? correlationIdSanitized,
        int statusCode,
        long elapsedMs)
    {
        if (correlationIdSanitized is null)
        {
            logger.LogInformation(
                CompletedEvent,
                "HTTP request finished {HttpMethod} {HttpPath} {HttpStatusCode} {ElapsedMilliseconds}",
                methodSanitized,
                pathSanitized,
                statusCode,
                elapsedMs);

            return;
        }

        logger.LogInformation(
            CompletedEvent,
            "HTTP request finished {HttpMethod} {HttpPath} {HttpCorrelationId} {HttpStatusCode} {ElapsedMilliseconds}",
            methodSanitized,
            pathSanitized,
            correlationIdSanitized,
            statusCode,
            elapsedMs);
    }
}
