using ArchLucid.Api.Models;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Diagnostics;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Admin;

/// <summary>
/// Accepts operator-shell client error reports for structured Serilog emission (no persistence).
/// </summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/diagnostics")]
[EnableRateLimiting("fixed")]
public sealed class ClientErrorTelemetryController(ILogger<ClientErrorTelemetryController> logger) : ControllerBase
{
    private const int MaxMessageLength = 500;

    private const int MaxStackLength = 2000;

    private const int MaxPathnameLength = 200;

    private const int MaxUserAgentLength = 500;

    private const int MaxContextEntries = 10;

    private const int MaxContextKeyLength = 50;

    private const int MaxContextValueLength = 200;

    /// <summary>Records a client-side error report at Warning level (sanitized).</summary>
    [HttpPost("client-error")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult PostClientError([FromBody] ClientErrorReport? body)
    {
        if (body is null)
        {
            return this.BadRequestProblem("Request body is required.", ProblemTypes.ValidationFailed);
        }

        string message = body.Message?.Trim() ?? string.Empty;

        if (message.Length == 0)
        {
            return this.BadRequestProblem("Message is required.", ProblemTypes.ValidationFailed);
        }

        if (message.Length > MaxMessageLength)
        {
            return this.BadRequestProblem(
                $"Message must be at most {MaxMessageLength} characters.",
                ProblemTypes.ValidationFailed);
        }

        string? stack = TruncateNullable(body.Stack, MaxStackLength);
        string? pathname = TruncateNullable(body.Pathname, MaxPathnameLength);
        string? userAgent = TruncateNullable(body.UserAgent, MaxUserAgentLength);
        string? timestampUtc = TruncateNullable(body.TimestampUtc, 64);

        if (body.Context is not null)
        {
            if (body.Context.Count > MaxContextEntries)
            {
                return this.BadRequestProblem(
                    $"Context may contain at most {MaxContextEntries} entries.",
                    ProblemTypes.ValidationFailed);
            }

            foreach (KeyValuePair<string, string> pair in body.Context)
            {
                if (pair.Key.Length > MaxContextKeyLength || pair.Value.Length > MaxContextValueLength)
                {
                    return this.BadRequestProblem(
                        $"Context keys must be at most {MaxContextKeyLength} characters and values at most {MaxContextValueLength} characters.",
                        ProblemTypes.ValidationFailed);
                }
            }
        }

        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning(
                "Operator shell client error: {ClientErrorMessage} | Path={ClientErrorPathname} | UA={ClientErrorUserAgent} | At={ClientErrorTimestamp} | Stack={ClientErrorStack}",
                LogSanitizer.Sanitize(message),
                LogSanitizer.Sanitize(pathname ?? string.Empty),
                LogSanitizer.Sanitize(userAgent ?? string.Empty),
                LogSanitizer.Sanitize(timestampUtc ?? string.Empty),
                LogSanitizer.Sanitize(stack ?? string.Empty));
        }

        return NoContent();
    }

    private static string? TruncateNullable(string? value, int maxLen)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        string trimmed = value.Trim();

        if (trimmed.Length <= maxLen)
        {
            return trimmed;
        }

        return trimmed[..maxLen];
    }
}
