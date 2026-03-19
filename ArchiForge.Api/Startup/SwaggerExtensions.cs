using Microsoft.Extensions.DependencyInjection;

namespace ArchiForge.Api.Startup;

internal static class SwaggerExtensions
{
    public static IServiceCollection AddArchiForgeSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new()
            {
                Title = "ArchiForge API",
                Version = "v1",
                Description = "API for orchestrating AI-driven architecture design. See docs/API_CONTRACTS.md for notable behaviors: 422 (comparison verification failed), 404 run-not-found, 409 conflict, and request validation (400)."
            });
            c.OperationFilter<Swagger.ReplayExamplesOperationFilter>();
            c.OperationFilter<Swagger.ComparisonHistoryQueryOperationFilter>();
            c.OperationFilter<Swagger.ProblemDetailsResponsesOperationFilter>();
        });
        return services;
    }
}
