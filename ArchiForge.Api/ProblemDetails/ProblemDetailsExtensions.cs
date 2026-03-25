using ArchiForge.Application;

using Microsoft.AspNetCore.Mvc;

namespace ArchiForge.Api.ProblemDetails;

/// <summary>
/// Extension methods for returning RFC 7807 Problem Details from controllers.
/// </summary>
public static class ProblemDetailsExtensions
{
    private const string ProblemJsonMediaType = ApplicationProblemMapper.ProblemJsonMediaType;

    /// <summary>
    /// Returns 400 Bad Request with a Problem Details body.
    /// </summary>
    public static IActionResult BadRequestProblem(
        this ControllerBase controller,
        string detail,
        string? type = null,
        string? instance = null)
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Type = type ?? ProblemTypes.BadRequest,
            Title = "Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Detail = detail,
            Instance = instance ?? controller.Request.Path
        };
        return new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { ProblemJsonMediaType }
        };
    }

    /// <summary>
    /// Returns 404 Not Found with a Problem Details body.
    /// </summary>
    public static IActionResult NotFoundProblem(
        this ControllerBase controller,
        string detail,
        string? type = null,
        string? instance = null)
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Type = type ?? ProblemTypes.ResourceNotFound,
            Title = "Not Found",
            Status = StatusCodes.Status404NotFound,
            Detail = detail,
            Instance = instance ?? controller.Request.Path
        };
        return new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { ProblemJsonMediaType }
        };
    }

    public static IActionResult ConflictProblem(
        this ControllerBase controller,
        string detail,
        string? type = null,
        string? instance = null)
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Type = type ?? ProblemTypes.Conflict,
            Title = "Conflict",
            Status = StatusCodes.Status409Conflict,
            Detail = detail,
            Instance = instance ?? controller.Request.Path
        };
        return new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { ProblemJsonMediaType }
        };
    }

    /// <summary>
    /// Converts common InvalidOperationException variants to consistent ProblemDetails.
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
            badRequestType);
    }
}
