using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace ArchLucid.Api.Swagger;

/// <summary>
///     Documents the synthetic CloudEvents-shaped POST body emitted by the dry-run probe (for ITSM / SIEM integrators).
/// </summary>
public sealed class OutboundWebhookDryRunExamplesOperationFilter : IOperationFilter
{
    private const string RequestExample =
        """
        {
          "targetUrl": "https://webhook.example.com/archlucid",
          "sharedSecret": "rotate-this-in-real-integrations"
        }
        """;

    private const string CloudEventShape =
        """
        {
          "specversion": "1.0",
          "type": "com.archlucid.finding.created.sample",
          "source": "https://api.archlucid.local/v1/webhooks/dry-run",
          "id": "11111111-1111-1111-1111-111111111111",
          "time": "2026-05-05T12:00:00.0000000Z",
          "datacontenttype": "application/json",
          "data": {
            "tenantId": "00000000-0000-0000-0000-000000000000",
            "findingId": "22222222-2222-2222-2222-222222222222",
            "runId": "33333333-3333-3333-3333-333333333333",
            "note": "Synthetic webhook dry-run (no persistence); validate signature + payload at your subscriber."
          }
        }
        """;

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!string.Equals(context.MethodInfo?.Name, nameof(Controllers.Webhooks.OutboundWebhookDryRunController.DryRunAsync),
                StringComparison.Ordinal))
            return;

        string block =
            "\n\n### Example `OutboundWebhookDryRunRequest` (JSON)\n\n```json\n"
            + RequestExample.Trim()
            + "\n```\n\n### Synthetic CloudEvents envelope (POST body to subscriber)\n\n```json\n"
            + CloudEventShape.Trim()
            + "\n```\n\nSee `ArchLucid.Api.Services.OutboundWebhookDryRunService.BuildSyntheticWebhookBodyUtf8` and `docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md`. ";

        operation.Description = string.IsNullOrWhiteSpace(operation.Description)
            ? block.Trim()
            : operation.Description.TrimEnd() + block;
    }
}
