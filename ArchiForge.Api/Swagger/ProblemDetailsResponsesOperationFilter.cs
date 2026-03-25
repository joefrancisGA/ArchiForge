using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace ArchiForge.Api.Swagger;

/// <summary>Enriches 404/409 responses with problem-details type hints for OpenAPI docs.</summary>
public sealed class ProblemDetailsResponsesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Responses == null)
            return;

        string path = (context.ApiDescription.RelativePath ?? "").ToLowerInvariant();

        if (operation.Responses.TryGetValue("404", out IOpenApiResponse? notFound))
        {
            if (path.Contains("run/") && (path.Contains("compare") || path.Contains("commit") || path.Contains("execute") || path.Contains("replay") || path.Contains("/run/")))
                notFound.Description = (notFound.Description ?? "").TrimEnd() + " Problem type: `#run-not-found` when the referenced run does not exist.";
            if (path.Contains("comparisons"))
                notFound.Description = (notFound.Description ?? "").TrimEnd() + " Problem type: `#run-not-found` when a referenced run is missing.";
            if (path.Contains("policy-packs") && path.Contains("assign"))
                notFound.Description = (notFound.Description ?? "").TrimEnd() + " Problem type: `#policy-pack-version-not-found` when the pack has no row for the requested version.";
        }

        if (operation.Responses.TryGetValue("409", out IOpenApiResponse? conflict))
            conflict.Description = (conflict.Description ?? "").TrimEnd() + " Problem type: `#conflict` (e.g. commit when run is in Failed state or already committed).";
    }
}
