using ArchLucid.Application;
using ArchLucid.Contracts.Governance;
using ArchLucid.Decisioning.Validation;

using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.ProblemDetails;

/// <summary>
///     Extension methods for returning RFC 9457 Problem Details from controllers (obsoletes RFC 7807).
/// </summary>
public static class ProblemDetailsExtensions
{
    private const string ProblemJsonMediaType = ApplicationProblemMapper.ProblemJsonMediaType;

    /// <summary>
    ///     Returns 400 Bad Request with a Problem Details body.
    /// </summary>
    public static IActionResult BadRequestProblem(
        this ControllerBase controller,
        string detail,
        string? type = null,
        string? instance = null)
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new()
        {
            Type = type ?? ProblemTypes.BadRequest,
            Title = "Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Detail = detail,
            Instance = instance ?? controller.Request.Path
        };
        ProblemErrorCodes.AttachErrorCode(problem, problem.Type);
        ProblemSupportHints.AttachForProblemType(problem);
        ProblemCorrelation.Attach(problem, controller.HttpContext);
        return new ObjectResult(problem) { StatusCode = problem.Status, ContentTypes = { ProblemJsonMediaType } };
    }

    /// <summary>
    ///     Returns 404 Not Found with a Problem Details body.
    /// </summary>
    public static IActionResult NotFoundProblem(
        this ControllerBase controller,
        string detail,
        string? type = null,
        string? instance = null)
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new()
        {
            Type = type ?? ProblemTypes.ResourceNotFound,
            Title = "Not Found",
            Status = StatusCodes.Status404NotFound,
            Detail = detail,
            Instance = instance ?? controller.Request.Path
        };
        ProblemErrorCodes.AttachErrorCode(problem, problem.Type);
        ProblemSupportHints.AttachForProblemType(problem);
        ProblemCorrelation.Attach(problem, controller.HttpContext);
        return new ObjectResult(problem) { StatusCode = problem.Status, ContentTypes = { ProblemJsonMediaType } };
    }

    public static IActionResult ConflictProblem(
        this ControllerBase controller,
        string detail,
        string? type = null,
        string? instance = null)
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new()
        {
            Type = type ?? ProblemTypes.Conflict,
            Title = "Conflict",
            Status = StatusCodes.Status409Conflict,
            Detail = detail,
            Instance = instance ?? controller.Request.Path
        };
        ProblemErrorCodes.AttachErrorCode(problem, problem.Type);
        ProblemSupportHints.AttachForProblemType(problem);
        ProblemCorrelation.Attach(problem, controller.HttpContext);
        return new ObjectResult(problem) { StatusCode = problem.Status, ContentTypes = { ProblemJsonMediaType } };
    }

    /// <summary>Returns 400 when the committed golden manifest fails JSON Schema validation.</summary>
    public static IActionResult GoldenManifestSchemaValidationProblem(
        this ControllerBase controller,
        SchemaValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new()
        {
            Type = ProblemTypes.ValidationFailed,
            Title = "Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Detail = result.Errors.Count == 0
                ? "Golden manifest schema validation failed."
                : string.Join(
                    "; ",
                    result.Errors.Count <= 5
                        ? result.Errors
                        : result.Errors.Take(5).Concat(new[] { $"(+{result.Errors.Count - 5} more)" })),
            Instance = controller.Request.Path.Value,
            Extensions =
            {
                ["errors"] = result.Errors.ToArray(),
            }
        };

        ProblemErrorCodes.AttachErrorCode(problem, problem.Type);
        ProblemSupportHints.AttachForProblemType(problem);
        ProblemCorrelation.Attach(problem, controller.HttpContext);
        return new ObjectResult(problem) { StatusCode = problem.Status, ContentTypes = { ProblemJsonMediaType } };
    }

    /// <summary>Returns 409 when optional pre-commit governance blocks commit.</summary>
    public static IActionResult GovernancePreCommitBlockedProblem(
        this ControllerBase controller,
        PreCommitGateResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new()
        {
            Type = ProblemTypes.GovernancePreCommitBlocked,
            Title = "Conflict",
            Status = StatusCodes.Status409Conflict,
            Detail = result.Reason ?? "Commit blocked by governance policy.",
            Instance = controller.Request.Path,
            Extensions = { ["blockingFindingIds"] = result.BlockingFindingIds.ToArray() }
        };

        if (result.PolicyPackId is not null)

            problem.Extensions["policyPackId"] = result.PolicyPackId;

        ProblemErrorCodes.AttachErrorCode(problem, problem.Type);
        ProblemSupportHints.AttachForProblemType(problem);
        ProblemCorrelation.Attach(problem, controller.HttpContext);
        return new ObjectResult(problem) { StatusCode = problem.Status, ContentTypes = { ProblemJsonMediaType } };
    }

    /// <summary>
    ///     Returns 422 Unprocessable Entity with a Problem Details body (e.g. batch replay where every ID failed).
    /// </summary>
    public static IActionResult UnprocessableEntityProblem(
        this ControllerBase controller,
        string detail,
        string? type = null,
        string? instance = null)
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new()
        {
            Type = type ?? ProblemTypes.ValidationFailed,
            Title = "Unprocessable Entity",
            Status = StatusCodes.Status422UnprocessableEntity,
            Detail = detail,
            Instance = instance ?? controller.Request.Path
        };
        ProblemErrorCodes.AttachErrorCode(problem, problem.Type);
        ProblemSupportHints.AttachForProblemType(problem);
        ProblemCorrelation.Attach(problem, controller.HttpContext);
        return new ObjectResult(problem) { StatusCode = problem.Status, ContentTypes = { ProblemJsonMediaType } };
    }

    /// <summary>
    ///     Returns 413 Payload Too Large with Problem Details (e.g. graph node count exceeds full-response limit).
    /// </summary>
    public static IActionResult PayloadTooLargeProblem(
        this ControllerBase controller,
        string detail,
        string? type = null,
        string? instance = null)
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new()
        {
            Type = type ?? ProblemTypes.GraphTooLargeForFullResponse,
            Title = "Payload Too Large",
            Status = StatusCodes.Status413PayloadTooLarge,
            Detail = detail,
            Instance = instance ?? controller.Request.Path
        };
        ProblemErrorCodes.AttachErrorCode(problem, problem.Type);
        ProblemSupportHints.AttachForProblemType(problem);
        ProblemCorrelation.Attach(problem, controller.HttpContext);
        return new ObjectResult(problem) { StatusCode = problem.Status, ContentTypes = { ProblemJsonMediaType } };
    }

    /// <summary>
    ///     Returns 500 Internal Server Error with a Problem Details body. Use only for genuine server-side faults
    ///     where the caller cannot recover by changing the request — transient downstream failures should prefer
    ///     <see cref="ServiceUnavailableProblem(ControllerBase, string, string?, string?)" /> so clients retry.
    /// </summary>
    public static IActionResult InternalServerErrorProblem(
        this ControllerBase controller,
        string detail,
        string? type = null,
        string? instance = null)
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new()
        {
            Type = type ?? ProblemTypes.InternalError,
            Title = "Internal Server Error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = detail,
            Instance = instance ?? controller.Request.Path
        };
        ProblemErrorCodes.AttachErrorCode(problem, problem.Type);
        ProblemSupportHints.AttachForProblemType(problem);
        ProblemCorrelation.Attach(problem, controller.HttpContext);
        return new ObjectResult(problem) { StatusCode = problem.Status, ContentTypes = { ProblemJsonMediaType } };
    }

    /// <summary>
    ///     Returns 503 Service Unavailable with a Problem Details body (e.g. database timeout, transient downstream failure).
    /// </summary>
    public static IActionResult ServiceUnavailableProblem(
        this ControllerBase controller,
        string detail,
        string? type = null,
        string? instance = null)
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new()
        {
            Type = type ?? ProblemTypes.DatabaseUnavailable,
            Title = "Service Unavailable",
            Status = StatusCodes.Status503ServiceUnavailable,
            Detail = detail,
            Instance = instance ?? controller.Request.Path
        };
        ProblemErrorCodes.AttachErrorCode(problem, problem.Type);
        ProblemSupportHints.AttachForProblemType(problem);
        ProblemCorrelation.Attach(problem, controller.HttpContext);
        return new ObjectResult(problem) { StatusCode = problem.Status, ContentTypes = { ProblemJsonMediaType } };
    }

    /// <summary>
    ///     Converts common InvalidOperationException variants to consistent ProblemDetails.
    /// </summary>
    public static IActionResult InvalidOperationProblem(
        this ControllerBase controller,
        InvalidOperationException exception,
        string badRequestType)
    {
        if (exception is ConflictException)
            return controller.ConflictProblem(exception.Message, ProblemTypes.Conflict);

        string? instance = controller.Request.Path.Value;
        return ApplicationProblemMapper.MapInvalidOperation(
            exception,
            instance,
            badRequestType,
            controller.HttpContext);
    }
}
