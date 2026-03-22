using ArchiForge.Decisioning.Governance.PolicyPacks;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ArchiForge.Api.Swagger;

/// <summary>Enriches the generated OpenAPI schema for merged policy content (see also operation examples on policy pack POSTs).</summary>
public sealed class PolicyPackContentDocumentSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(PolicyPackContentDocument))
            return;

        schema.Description =
            "Declarative governance payload. Empty `complianceRuleIds` and `complianceRuleKeys` mean no compliance filter; "
            + "empty `alertRuleIds` / `compositeAlertRuleIds` mean no alert filter. "
            + "`advisoryDefaults` and `metadata` merge last-wins per key across assigned packs. "
            + "Example JSON is shown on POST /v1/policy-packs (create/publish) in Swagger operation descriptions.";
    }
}
