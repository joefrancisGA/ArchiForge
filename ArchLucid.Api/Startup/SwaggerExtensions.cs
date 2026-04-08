using ArchLucid.Api.Swagger;

using Microsoft.AspNetCore.Mvc.Controllers;

namespace ArchLucid.Api.Startup;

internal static class SwaggerExtensions
{
    public static IServiceCollection AddArchLucidSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            // Disambiguate types that share a short name (e.g. Decisioning vs Contracts DecisionTrace).
            c.CustomSchemaIds(type => type.FullName?.Replace("+", ".", StringComparison.Ordinal) ?? type.Name);

            c.TagActionsBy(api =>
            {
                if (api.ActionDescriptor is not ControllerActionDescriptor cad) return [api.GroupName ?? "API"];

                string tag = cad.ControllerName switch
                {
                    "PolicyPacks" or "Governance" or "GovernancePreview" or "GovernanceResolution" => "Governance",
                    "AuthorityQuery" or "AuthorityCompare" or "AuthorityReplay" => "Authority",
                    "Advisory" or "AdvisoryScheduling" => "Advisory",
                    "Retrieval" => "Retrieval",
                    "Ask" or "Conversation" => "Ask & conversations",
                    "Graph" or "Manifests" or "Provenance" or "ProvenanceQuery" or "ArtifactExport" => "Authority artifacts",
                    "Comparison" or "Comparisons" or "RunComparison" => "Comparison",
                    "Explanation" or "AnalysisReports" or "DocxExport" or "Exports" => "Analysis & export",
                    "Runs" or "Jobs" => "Architecture runs",
                    "AlertRules" or "Alerts" or "AlertSimulation" or "AlertTuning" or "CompositeAlertRules"
                        or "AlertRoutingSubscriptions" => "Alerts & routing",
                    "DigestSubscriptions" => "Digest subscriptions",
                    "RecommendationLearning" => "Advisory learning",
                    "ProductLearning" => "Product learning",
                    "Audit" => "Audit",
                    "Diagnostics" or "Docs" or "AuthDebug" or "ScopeDebug" or "Demo" => "Diagnostics & debug",
                    _ => cad.ControllerName,
                };
                return [tag];
            });

            c.SwaggerDoc("v1", new()
            {
                Title = "ArchLucid API",
                Version = "v1",
                Description = "API for orchestrating AI-driven architecture design. URL versioning: /v1/... (default 1.0). See docs/API_CONTRACTS.md for versioning, correlation ID (X-Correlation-ID), 422 (comparison verification failed), 404 run-not-found, 409 conflict, and request validation (400). Create-run body may include context-ingestion fields (inline requirements, documents, policy/topology/security hints); see docs/CONTEXT_INGESTION.md and the POST /v1/architecture/request example. Governance: /v1/policy-packs (effective-content merge, compliance/alert filtering); operator alerts: /v1/alerts, /v1/alert-rules, /v1/composite-alert-rules, /v1/alert-simulation, /v1/alert-tuning, /v1/alert-routing-subscriptions, /v1/digest-subscriptions."
            });
            c.OperationFilter<ReplayExamplesOperationFilter>();
            c.OperationFilter<ArchitectureRequestExamplesOperationFilter>();
            c.OperationFilter<ComparisonHistoryQueryOperationFilter>();
            c.OperationFilter<ProblemDetailsResponsesOperationFilter>();
            c.OperationFilter<PolicyPackExamplesOperationFilter>();
            c.OperationFilter<AlertExamplesOperationFilter>();
            c.SchemaFilter<PolicyPackContentDocumentSchemaFilter>();
            c.DocumentFilter<OpenApiAuthSecurityDocumentFilter>();
            c.OperationFilter<OpenApiAuthSecurityOperationFilter>();
        });
        return services;
    }
}
