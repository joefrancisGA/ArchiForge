using System.Reflection;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace ArchiForge.Api.Swagger;

/// <summary>
/// Clears inherited document-level <c>security</c> for endpoints that allow anonymous access
/// (e.g. static docs HTML). When <see cref="SwaggerOpenApiAuth"/> resolves no scheme, this is a no-op.
/// </summary>
public sealed class OpenApiAuthSecurityOperationFilter(IConfiguration configuration) : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        string? schemeId = SwaggerOpenApiAuth.ResolveSecuritySchemeId(configuration);

        if (string.IsNullOrEmpty(schemeId))
        {
            return;
        }

        if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor cad)
        {
            return;
        }

        if (!AllowsAnonymous(cad))
        {
            return;
        }

        // OpenAPI 3: empty array = no auth for this operation (overrides root security).
        operation.Security = [];
    }

    private static bool AllowsAnonymous(ControllerActionDescriptor cad)
    {
        IList<Microsoft.AspNetCore.Mvc.Filters.FilterDescriptor>? filters = cad.FilterDescriptors;

        if (filters != null && filters.Any(static f => f.Filter is IAllowAnonymousFilter))
        {
            return true;
        }

        if (cad.EndpointMetadata.Any(static m => m is AllowAnonymousAttribute))
        {
            return true;
        }

        if (cad.MethodInfo.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true) != null)
        {
            return true;
        }

        return cad.ControllerTypeInfo.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true) != null;
    }
}
