using System.Data.Common;

using ArchLucid.AgentRuntime;
using ArchLucid.Application;
using ArchLucid.Application.Analysis;
using ArchLucid.Core.Resilience;
using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Repositories;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ArchLucid.Api.ProblemDetails;

/// <summary>
/// Single mapping path from application exceptions to <see cref="ObjectResult"/> problem+json responses.
/// Used by <see cref="ApiProblemDetailsExceptionFilter"/> and <see cref="ProblemDetailsExtensions.InvalidOperationProblem"/>.
/// </summary>
public static class ApplicationProblemMapper
{
    public const string ProblemJsonMediaType = "application/problem+json";

    /// <summary>
    /// Maps exceptions handled globally by the API (filter). Returns false if not mapped.
    /// </summary>
    public static bool TryMapUnhandledException(Exception ex, HttpContext httpContext, out ObjectResult? result)
    {
        result = null;
        string? instance = httpContext.Request.Path.Value;

        if (ex is TrialLimitExceededException tlex)
        {
            result = TrialLimitProblemResponse.CreateResult(tlex, instance, httpContext);

            return true;
        }

        if (ex is ComparisonVerificationFailedException cvf)
        {
            result = MapComparisonVerificationFailed(cvf, instance, httpContext);
            return true;
        }

        if (ex is ConflictException cex)
        {
            result = CreateProblemResult(
                StatusCodes.Status409Conflict,
                "Conflict",
                cex.Message,
                ProblemTypes.Conflict,
                instance,
                httpContext);
            return true;
        }

        if (ex is RunNotFoundException rnf)
        {
            result = CreateProblemResult(
                StatusCodes.Status404NotFound,
                "Run Not Found",
                rnf.Message,
                ProblemTypes.RunNotFound,
                instance,
                httpContext);
            return true;
        }

        if (TryMapDatabaseException(ex, instance, httpContext, out result))
            return true;

        if (ex is CircuitBreakerOpenException cbo)
        {
            result = CreateProblemResult(
                StatusCodes.Status503ServiceUnavailable,
                "AI Service Temporarily Unavailable",
                cbo.Message,
                ProblemTypes.CircuitBreakerOpen,
                instance,
                httpContext,
                details =>
                {
                    if (cbo.RetryAfterUtc is { } until)
                        details.Extensions["retryAfterUtc"] = until;
                });

            return true;
        }

        if (ex is RunConcurrencyConflictException rcc)
        {
            result = CreateProblemResult(
                StatusCodes.Status409Conflict,
                "Concurrency conflict",
                rcc.Message,
                ProblemTypes.Conflict,
                instance,
                httpContext);
            return true;
        }

        if (ex is LlmTokenQuotaExceededException quotaEx)
        {
            result = CreateProblemResult(
                StatusCodes.Status429TooManyRequests,
                "LLM token quota exceeded",
                quotaEx.Message,
                ProblemTypes.LlmTokenQuotaExceeded,
                instance,
                httpContext);
            return true;
        }

        if (ex is InvalidOperationException ioe)
        {
            result = MapInvalidOperation(ioe, instance, ProblemTypes.BadRequest, httpContext);
            return true;
        }

        if (ex is not (ArgumentException or ArgumentNullException))
            return false;

        result = CreateProblemResult(
            StatusCodes.Status400BadRequest,
            "Bad Request",
            ex.Message,
            ProblemTypes.ValidationFailed,
            instance,
            httpContext);
        return true;

    }

    /// <summary>
    /// Maps <see cref="InvalidOperationException"/> to a 400 Bad Request response.
    /// "Not found" scenarios must use typed exceptions (<see cref="RunNotFoundException"/>,
    /// <see cref="ConflictException"/>, etc.) so they are handled by
    /// <see cref="TryMapUnhandledException"/> before reaching this method.
    /// </summary>
    /// <param name="httpContext">Current request; used to stamp <see cref="ProblemCorrelation.ExtensionKey"/>.</param>
    public static ObjectResult MapInvalidOperation(
        InvalidOperationException ex,
        string? instance,
        string badRequestProblemType,
        HttpContext? httpContext)
    {
        return CreateProblemResult(
            StatusCodes.Status400BadRequest,
            "Bad Request",
            ex.Message,
            badRequestProblemType,
            instance,
            httpContext);
    }

    private static ObjectResult MapComparisonVerificationFailed(
        ComparisonVerificationFailedException cvf,
        string? instance,
        HttpContext httpContext)
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new()
        {
            Type = ProblemTypes.ComparisonVerificationFailed,
            Title = "Unprocessable Entity",
            Status = StatusCodes.Status422UnprocessableEntity,
            Detail = cvf.Message,
            Instance = string.IsNullOrWhiteSpace(instance) ? null : instance
        };

        ProblemErrorCodes.AttachErrorCode(problem, ProblemTypes.ComparisonVerificationFailed);
        ProblemSupportHints.AttachForProblemType(problem);
        ProblemCorrelation.Attach(problem, httpContext);

        if (cvf.Drift is not { } drift)
            return new ObjectResult(problem) { StatusCode = problem.Status, ContentTypes = { ProblemJsonMediaType } };

        problem.Extensions["driftDetected"] = drift.DriftDetected;
        if (!string.IsNullOrWhiteSpace(drift.Summary))
            problem.Extensions["driftSummary"] = drift.Summary;

        return new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { ProblemJsonMediaType }
        };
    }

    /// <summary>
    /// Maps SQL Server timeouts (<see cref="SqlException"/> with <c>Number == -2</c>),
    /// deadlock victims (<c>1205</c>) to <see cref="StatusCodes.Status409Conflict"/>,
    /// <see cref="TimeoutException"/> to 503, and other <see cref="DbException"/> to 503 Service Unavailable.
    /// </summary>
    /// <remarks>
    /// <see cref="SqlException.Number"/> <c>-2</c> is the canonical SQL Server timeout error code.
    /// Deadlock (<c>1205</c>) from parallel writers is treated like other commit races so clients receive 409, not 503.
    /// Remaining database-origin exceptions are surfaced as retryable 503 so clients and load balancers
    /// can distinguish transient failures from permanent 500 errors.
    /// </remarks>
    public static bool TryMapDatabaseException(
        Exception ex,
        string? instance,
        HttpContext httpContext,
        out ObjectResult? result)
    {
        result = null;

        if (ex is SqlException { Number: -2 })
        {
            result = CreateProblemResult(
                StatusCodes.Status503ServiceUnavailable,
                "Database Timeout",
                "The database query timed out. The request may succeed on retry.",
                ProblemTypes.DatabaseTimeout,
                instance,
                httpContext);
            return true;
        }

        // Parallel commits on the same run can deadlock; 409 matches other concurrency outcomes (ROWVERSION, unique key).
        if (TryFindSqlExceptionWithNumber(ex, 1205) is not null)
        {
            result = CreateProblemResult(
                StatusCodes.Status409Conflict,
                "Concurrency conflict",
                "The database transaction deadlocked with a concurrent request. Retry the commit.",
                ProblemTypes.Conflict,
                instance,
                httpContext);
            return true;
        }

        if (ex is TimeoutException)
        {
            result = CreateProblemResult(
                StatusCodes.Status503ServiceUnavailable,
                "Request Timeout",
                "An operation timed out. The request may succeed on retry.",
                ProblemTypes.DatabaseTimeout,
                instance,
                httpContext);
            return true;
        }

        if (ex is not DbException)
            return false;

        result = CreateProblemResult(
            StatusCodes.Status503ServiceUnavailable,
            "Database Unavailable",
            "The database is currently unreachable. The request may succeed on retry.",
            ProblemTypes.DatabaseUnavailable,
            instance,
            httpContext);

        return true;
    }

    /// <summary>Walks <see cref="Exception.InnerException"/> chain for a <see cref="SqlException"/> with the given <see cref="SqlException.Number"/>.</summary>
    private static SqlException? TryFindSqlExceptionWithNumber(Exception ex, int number)
    {
        for (Exception? e = ex; e is not null; e = e.InnerException)

            if (e is SqlException sql && sql.Number == number)
                return sql;


        return null;
    }

    public static ObjectResult CreateProblemResult(
        int statusCode,
        string title,
        string detail,
        string type,
        string? instance,
        HttpContext? httpContext,
        Action<Microsoft.AspNetCore.Mvc.ProblemDetails>? extend = null)
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new()
        {
            Type = type,
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = string.IsNullOrWhiteSpace(instance) ? null : instance
        };

        ProblemErrorCodes.AttachErrorCode(problem, type);
        extend?.Invoke(problem);
        ProblemSupportHints.AttachForProblemType(problem);
        ProblemCorrelation.Attach(problem, httpContext);

        return new ObjectResult(problem)
        {
            StatusCode = statusCode,
            ContentTypes = { ProblemJsonMediaType }
        };
    }
}
