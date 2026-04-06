using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace ArchiForge.Api.Swagger;

/// <summary>Adds description and a JSON example for POST create run (<see cref="ArchiForge.Contracts.Requests.ArchitectureRequest"/>) including context-ingestion fields.</summary>
/// <remarks>
/// Microsoft.OpenApi 2.x does not ship the old <c>OpenApiObject</c> / <c>Microsoft.OpenApi.Any</c> model for request-body examples;
/// a markdown JSON block in the operation description stays compatible with Swashbuckle and OpenAPI.NET v2.
/// </remarks>
public sealed class ArchitectureRequestExamplesOperationFilter : IOperationFilter
{
    private const string ExampleRequestJson =
        """
        {
          "requestId": "REQ-SWAGGER-INGEST-001",
          "description": "Design a secure Azure workload with private networking, managed identity, and audit logging for compliance.",
          "systemName": "billing-api",
          "environment": "prod",
          "cloudProvider": 1,
          "constraints": [ "Private endpoints required" ],
          "requiredCapabilities": [ "SQL", "Managed Identity" ],
          "assumptions": [],
          "inlineRequirements": [ "Must support regional failover" ],
          "documents": [
            {
              "name": "controls.txt",
              "contentType": "text/plain",
              "content": "REQ: HA deployment\nPOL: SOC2 controls apply"
            }
          ],
          "policyReferences": [ "ORG-SEC-001" ],
          "topologyHints": [ "ingress in subnet-frontend" ],
          "securityBaselineHints": [ "encrypt data at rest" ],
          "infrastructureDeclarations": [
            {
              "name": "core.json",
              "format": "json",
              "content": "{ \"resources\": [ { \"type\": \"vnet\", \"name\": \"core-vnet\", \"region\": \"eastus\", \"properties\": { \"addressSpace\": \"10.0.0.0/16\" } } ] }"
            }
          ]
        }
        """;

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!string.Equals(context.MethodInfo?.Name, "CreateRun", StringComparison.Ordinal))
            return;

        string extra =
            "**Context ingestion (optional):** "
            + "`inlineRequirements`, `documents` (inline `name` + `contentType` + `content` — see supported types in `SupportedContextDocumentContentTypes`), "
            + "`policyReferences`, `topologyHints`, `securityBaselineHints`, `infrastructureDeclarations` (`format`: `json` | `simple-terraform`). "
            + "Plain-text/markdown documents may use line prefixes `REQ:`, `POL:`, `TOP:`, `SEC:` (see `docs/CONTEXT_INGESTION.md`). "
            + "Structured IaC snippets become canonical topology/security objects before graph build.";

        string exampleBlock =
            "\n\n### Example request body (JSON)\n\n```json\n"
            + ExampleRequestJson.Trim()
            + "\n```\n";

        operation.Summary ??= "Create an architecture run";
        operation.Description = string.IsNullOrWhiteSpace(operation.Description)
            ? extra + exampleBlock
            : operation.Description.TrimEnd() + " " + extra + exampleBlock;
    }
}
