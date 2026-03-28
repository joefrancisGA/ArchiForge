using System.Data.Common;

using ArchiForge.Application;
using ArchiForge.Application.Analysis;
using ArchiForge.Core.Resilience;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ArchiForge.Api.ProblemDetails;

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
    public static bool TryMapUnhandledException(Exception ex, string? instance, out ObjectResult? result)
    {
        result = null;

        if (ex is ComparisonVerificationFailedException cvf)
        {
            result = MapComparisonVerificationFailed(cvf, instance);
            return true;
        }

        if (ex is ConflictException cex)
        {
            result = CreateProblemResult(
                StatusCodes.Status409Conflict,
                "Conflict",
                cex.Message,
                ProblemTypes.Conflict,
                instance);
            return true;
        }

        if (ex is RunNotFoundException rnf)
        {
            result = CreateProblemResult(
                StatusCodes.Status404NotFound,
                "Run Not Found",
                rnf.Message,
                ProblemTypes.RunNotFound,
                instance);
            return true;
        }

        if (TryMapDatabaseException(ex, instance, out result))
            return true;

        if (ex is CircuitBreakerOpenException cbo)
        {
            result = CreateProblemResult(
                StatusCodes.Status503ServiceUnavailable,
                "AI Service Temporarily Unavailable",
                cbo.Message,
                ProblemTypes.CircuitBreakerOpen,
                instance);
            if (result.Value is Microsoft.AspNetCore.Mvc.ProblemDetails details && cbo.RetryAfterUtc is { } until)
                details.Extensions["retryAfterUtc"] = until;

            return true;
        }

        if (ex is InvalidOperationException ioe)
        {
            result = MapInvalidOperation(ioe, instance, ProblemTypes.BadRequest);
            return true;
        }

        if (ex is not (ArgumentException or ArgumentNullException)) return false;
        
        result = CreateProblemResult(
            StatusCodes.Status400BadRequest,
            "Bad Request",
            ex.Message,
            ProblemTypes.ValidationFailed,
            instance);
        return true;

    }

    /// <summary>
    /// Maps <see cref="InvalidOperationException"/> to a 400 Bad Request response.
    /// "Not found" scenarios must use typed exceptions (<see cref="RunNotFoundException"/>,
    /// <see cref="ConflictException"/>, etc.) so they are handled by
    /// <see cref="TryMapUnhandledException"/> before reaching this method.
    /// </summary>
    public static ObjectResult MapInvalidOperation(
        InvalidOperationException ex,
        string? instance,
        string badRequestProblemType)
    {
        return CreateProblemResult(
            StatusCodes.Status400BadRequest,
            "Bad Request",
            ex.Message,
            badRequestProblemType,
            instance);
    }

    private static ObjectResult MapComparisonVerificationFailed(
        ComparisonVerificationFailedException cvf,
        string? instance)
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new()
        {
            Type = ProblemTypes.ComparisonVerificationFailed,
            Title = "Unprocessable Entity",
            Status = StatusCodes.Status422UnprocessableEntity,
            Detail = cvf.Message,
            Instance = string.IsNullOrWhiteSpace(instance) ? null : instance
        };

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
    /// generic <see cref="TimeoutException"/>, and <see cref="DbException"/> to 503 Service Unavailable.
    /// </summary>
    /// <remarks>
    /// <see cref="SqlException.Number"/> <c>-2</c> is the canonical SQL Server timeout error code.
    /// All database-origin exceptions are surfaced as retryable 503 so clients and load balancers
    /// can distinguish transient failures from permanent 500 errors.
    /// </remarks>
    public static bool TryMapDatabaseException(Exception ex, string? instance, out ObjectResult? result)
    {
        result = null;

        if (ex is SqlException sqlEx && sqlEx.Number == -2)
        {
            result = CreateProblemResult(
                StatusCodes.Status503ServiceUnavailable,
                "Database Timeout",
                "The database query timed out. The request may succeed on retry.",
                ProblemTypes.DatabaseTimeout,
                instance);
            return true;
        }

        if (ex is TimeoutException)
        {
            result = CreateProblemResult(
                StatusCodes.Status503ServiceUnavailable,
                "Request Timeout",
                "An operation timed out. The request may succeed on retry.",
                ProblemTypes.DatabaseTimeout,
                instance);
            return true;
        }

        if (ex is DbException)
        {
            result = CreateProblemResult(
                StatusCodes.Status503ServiceUnavailable,
                "Database Unavailable",
                "The database is currently unreachable. The request may succeed on retry.",
                ProblemTypes.DatabaseUnavailable,
                instance);
            return true;
        }

        return false;
    }

    public static ObjectResult CreateProblemResult(
        int statusCode,
        string title,
        string detail,
        string type,
        string? instance)
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new()
        {
            Type = type,
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = string.IsNullOrWhiteSpace(instance) ? null : instance
        };

        return new ObjectResult(problem)
        {
            StatusCode = statusCode,
            ContentTypes = { ProblemJsonMediaType }
        };
    }
}
