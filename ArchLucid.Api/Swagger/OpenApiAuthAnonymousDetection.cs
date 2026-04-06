using System.Reflection;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace ArchiForge.Api.Swagger;

/// <summary>
/// Detects MVC actions that allow anonymous access for OpenAPI <c>security: []</c> overrides.
/// </summary>
internal static class OpenApiAuthAnonymousDetection
{
    internal static bool AllowsAnonymous(ControllerActionDescriptor cad)
    {
        IList<Microsoft.AspNetCore.Mvc.Filters.FilterDescriptor> filters = cad.FilterDescriptors;

        if (filters.Any(static f => f.Filter is IAllowAnonymousFilter))
        
            return true;
        

        if (cad.EndpointMetadata.Any(static m => m is AllowAnonymousAttribute))
        
            return true;
        

        if (cad.MethodInfo.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true) != null)
        
            return true;
        

        return cad.ControllerTypeInfo.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true) != null;
    }
}
