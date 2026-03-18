using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
 
namespace ArchiForge.Api.ProblemDetails;
 
/// <summary>
/// Maps common application exceptions to RFC 7807 Problem Details responses.
/// Keeps controllers focused on HTTP mapping by centralizing exception handling.
/// </summary>
public sealed class ApiProblemDetailsExceptionFilter : IExceptionFilter
{
    private const string ProblemJsonMediaType = "application/problem+json";

    public void OnException(ExceptionContext context)
    {
        if (context.ExceptionHandled)
        {
            return;
        }
 
        var ex = context.Exception;
        var instance = context.HttpContext?.Request?.Path.Value;
 
        if (ex is InvalidOperationException ioe)
        {
            // Convention used widely across services: "not found" indicates a 404.
            if (ioe.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = CreateProblemResult(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: ioe.Message,
                    type: ProblemTypes.ResourceNotFound,
                    instance: instance);
                context.ExceptionHandled = true;
                return;
            }
 
            context.Result = CreateProblemResult(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: ioe.Message,
                type: ProblemTypes.BadRequest,
                instance: instance);
            context.ExceptionHandled = true;
            return;
        }
 
        if (ex is ArgumentException or ArgumentNullException)
        {
            context.Result = CreateProblemResult(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: ex.Message,
                type: ProblemTypes.ValidationFailed,
                instance: instance);
            context.ExceptionHandled = true;
            return;
        }
    }

    private static ObjectResult CreateProblemResult(
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

