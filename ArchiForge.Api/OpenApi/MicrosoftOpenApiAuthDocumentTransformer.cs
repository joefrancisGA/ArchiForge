using ArchiForge.Api.Swagger;

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ArchiForge.Api.OpenApi;

/// <summary>
/// Aligns <c>MapOpenApi</c> output with Swashbuckle auth metadata for <c>/openapi/v1.json</c>.
/// </summary>
public sealed class MicrosoftOpenApiAuthDocumentTransformer(IConfiguration configuration) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        _ = context;
        OpenApiAuthDocumentMutator.Apply(document, configuration);
        return Task.CompletedTask;
    }
}
