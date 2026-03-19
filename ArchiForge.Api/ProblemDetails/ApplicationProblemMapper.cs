using ArchiForge.Application;
using ArchiForge.Application.Analysis;
using Microsoft.AspNetCore.Mvc;

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

        if (ex is InvalidOperationException ioe)
        {
            result = MapInvalidOperation(ioe, instance, ProblemTypes.BadRequest, notFoundTypeOverride: null);
            return true;
        }

        if (ex is ArgumentException or ArgumentNullException)
        {
            result = CreateProblemResult(
                StatusCodes.Status400BadRequest,
                "Bad Request",
                ex.Message,
                ProblemTypes.ValidationFailed,
                instance);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Maps <see cref="InvalidOperationException"/> for controller catch blocks (custom bad-request problem type).
    /// Does not handle <see cref="ConflictException"/> — callers should map conflict before calling this.
    /// </summary>
    public static ObjectResult MapInvalidOperation(
        InvalidOperationException ex,
        string? instance,
        string badRequestProblemType,
        string? notFoundTypeOverride)
    {
        if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            var type = notFoundTypeOverride ?? InferNotFoundProblemType(ex.Message);
            return CreateProblemResult(
                StatusCodes.Status404NotFound,
                "Not Found",
                ex.Message,
                type,
                instance);
        }

        return CreateProblemResult(
            StatusCodes.Status400BadRequest,
            "Bad Request",
            ex.Message,
            badRequestProblemType,
            instance);
    }

    /// <summary>
    /// When no explicit problem type is passed, treat messages that reference a run as <see cref="ProblemTypes.RunNotFound"/>;
    /// otherwise use <see cref="ProblemTypes.ResourceNotFound"/>.
    /// </summary>
    public static string InferNotFoundProblemType(string message)
    {
        if (!message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return ProblemTypes.ResourceNotFound;

        if (message.Contains("Run '", StringComparison.OrdinalIgnoreCase)
            || message.Contains(" for run '", StringComparison.OrdinalIgnoreCase)
            || message.Contains(" run '", StringComparison.OrdinalIgnoreCase))
            return ProblemTypes.RunNotFound;

        return ProblemTypes.ResourceNotFound;
    }

    private static ObjectResult MapComparisonVerificationFailed(
        ComparisonVerificationFailedException cvf,
        string? instance)
    {
        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Type = ProblemTypes.ComparisonVerificationFailed,
            Title = "Unprocessable Entity",
            Status = StatusCodes.Status422UnprocessableEntity,
            Detail = cvf.Message,
            Instance = string.IsNullOrWhiteSpace(instance) ? null : instance
        };

        if (cvf.Drift is { } drift)
        {
            problem.Extensions["driftDetected"] = drift.DriftDetected;
            if (!string.IsNullOrWhiteSpace(drift.Summary))
                problem.Extensions["driftSummary"] = drift.Summary;
        }

        return new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { ProblemJsonMediaType }
        };
    }

    public static ObjectResult CreateProblemResult(
        int statusCode,
        string title,
        string detail,
        string type,
        string? instance)
    {
        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
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
