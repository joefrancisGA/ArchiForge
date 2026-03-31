using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace ArchiForge.Api.Swagger;

/// <summary>
/// Registers OpenAPI <c>components.securitySchemes</c> from live <see cref="IConfiguration"/> so test hosts and
/// late configuration sources (e.g. <c>WebApplicationFactory</c>) see the correct <c>ArchiForgeAuth:Mode</c>.
/// </summary>
public sealed class OpenApiAuthSecurityDocumentFilter(IConfiguration configuration) : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        _ = context;
        OpenApiAuthDocumentMutator.Apply(swaggerDoc, configuration);
    }
}
