using Microsoft.AspNetCore.Mvc.Controllers;

namespace ArchiForge.Api.Startup;

internal static class SwaggerExtensions
{
    public static IServiceCollection AddArchiForgeSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.TagActionsBy(api =>
            {
                if (api.ActionDescriptor is not ControllerActionDescriptor cad) return [api.GroupName ?? "API"];
                
                var tag = cad.ControllerName switch
                {
                    "PolicyPacks" => "Governance",
                    "AlertRules" or "Alerts" or "AlertSimulation" or "AlertTuning" or "CompositeAlertRules"
                        or "AlertRoutingSubscriptions" => "Alerts & routing",
                    "DigestSubscriptions" => "Digest subscriptions",
                    _ => cad.ControllerName,
                };
                return [tag];

            });

            c.SwaggerDoc("v1", new()
            {
                Title = "ArchiForge API",
                Version = "v1",
                Description = "API for orchestrating AI-driven architecture design. URL versioning: /v1/... (default 1.0). See docs/API_CONTRACTS.md for versioning, correlation ID (X-Correlation-ID), 422 (comparison verification failed), 404 run-not-found, 409 conflict, and request validation (400). Create-run body may include context-ingestion fields (inline requirements, documents, policy/topology/security hints); see docs/CONTEXT_INGESTION.md and the POST /v1/architecture/request example. Governance: /v1/policy-packs (effective-content merge, compliance/alert filtering); operator alerts: /v1/alerts, /v1/alert-rules, /v1/composite-alert-rules, /v1/alert-simulation, /v1/alert-tuning, /v1/alert-routing-subscriptions, /v1/digest-subscriptions."
            });
            c.OperationFilter<Swagger.ReplayExamplesOperationFilter>();
            c.OperationFilter<Swagger.ArchitectureRequestExamplesOperationFilter>();
            c.OperationFilter<Swagger.ComparisonHistoryQueryOperationFilter>();
            c.OperationFilter<Swagger.ProblemDetailsResponsesOperationFilter>();
            c.OperationFilter<Swagger.PolicyPackExamplesOperationFilter>();
            c.OperationFilter<Swagger.AlertExamplesOperationFilter>();
            c.SchemaFilter<Swagger.PolicyPackContentDocumentSchemaFilter>();
        });
        return services;
    }
}
