using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ArchLucid.Api.ProblemDetails;

/// <summary>
/// Maps common application exceptions to RFC 7807 Problem Details responses.
/// Keeps controllers focused on HTTP mapping by centralizing exception handling.
/// </summary>
public sealed class ApiProblemDetailsExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.ExceptionHandled)
            return;

        if (!ApplicationProblemMapper.TryMapUnhandledException(context.Exception, context.HttpContext, out ObjectResult? result))
            return;
        
        context.Result = result;
        context.ExceptionHandled = true;
    }
}
