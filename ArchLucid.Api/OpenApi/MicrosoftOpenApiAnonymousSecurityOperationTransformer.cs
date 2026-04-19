using ArchLucid.Api.Swagger;

using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ArchLucid.Api.OpenApi;

/// <summary>
/// Clears inherited document <c>security</c> on <see cref="AllowAnonymousAttribute"/> actions in the Microsoft OpenAPI document.
/// </summary>
public sealed class MicrosoftOpenApiAnonymousSecurityOperationTransformer(IConfiguration configuration) : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        if (string.IsNullOrEmpty(SwaggerOpenApiAuth.ResolveSecuritySchemeId(configuration)))
            return Task.CompletedTask;
        

        if (context.Description.ActionDescriptor is not ControllerActionDescriptor cad)
            return Task.CompletedTask;
        

        if (!OpenApiAuthAnonymousDetection.AllowsAnonymous(cad))
            return Task.CompletedTask;
        

        operation.Security = [];
        return Task.CompletedTask;
    }
}
