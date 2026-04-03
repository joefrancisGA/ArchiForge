using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ArchiForge.Api.OpenApi;

/// <summary>
/// Post-processes the Microsoft OpenAPI document (<c>/openapi/v1.json</c>) for friendlier downstream
/// code generation (NSwag, Kiota, etc.).
/// </summary>
public sealed class MicrosoftOpenApiCodeGenFriendlySchemaTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        _ = context;
        _ = cancellationToken;
        OpenApiCodeGenFriendlySchemaMutator.Apply(document);
        return Task.CompletedTask;
    }
}
