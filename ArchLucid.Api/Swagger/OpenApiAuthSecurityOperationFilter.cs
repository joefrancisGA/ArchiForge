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
        if (string.IsNullOrEmpty(SwaggerOpenApiAuth.ResolveSecuritySchemeId(configuration)))
        
            return;
        

        if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor cad)
        
            return;
        

        if (!OpenApiAuthAnonymousDetection.AllowsAnonymous(cad))
        
            return;
        

        operation.Security = [];
    }
}
