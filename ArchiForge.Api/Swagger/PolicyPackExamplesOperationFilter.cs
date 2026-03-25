using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace ArchiForge.Api.Swagger;

/// <summary>Adds markdown JSON examples for policy pack create/publish/assign bodies (policy content JSON matches <c>PolicyPackContentDocument</c>).</summary>
public sealed class PolicyPackExamplesOperationFilter : IOperationFilter
{
    private const string CreateExample =
        """
        {
          "name": "Team security baseline",
          "description": "Restricts alerts and compliance checks for this workspace.",
          "packType": "ProjectCustom",
          "initialContentJson": "{\\n  \\"complianceRuleIds\\": [],\\n  \\"complianceRuleKeys\\": [\\"network-must-have-security-baseline\\"],\\n  \\"alertRuleIds\\": [],\\n  \\"compositeAlertRuleIds\\": [],\\n  \\"advisoryDefaults\\": { \\"scanDepth\\": \\"standard\\" },\\n  \\"metadata\\": { \\"tier\\": \\"gold\\" }\\n}"
        }
        """;

    private const string PublishExample =
        """
        {
          "version": "1.1.0",
          "contentJson": "{\\n  \\"complianceRuleKeys\\": [\\"network-must-have-security-baseline\\"],\\n  \\"alertRuleIds\\": [],\\n  \\"compositeAlertRuleIds\\": [],\\n  \\"advisoryDefaults\\": {},\\n  \\"metadata\\": {}\\n}"
        }
        """;

    private const string AssignExample =
        """
        {
          "version": "1.0.0"
        }
        """;

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        string? name = context.MethodInfo?.Name;
        string? example = name switch
        {
            "Create" => CreateExample,
            "Publish" => PublishExample,
            "Assign" => AssignExample,
            _ => null,
        };

        if (example is null)
            return;

        string controller = context.MethodInfo?.DeclaringType?.Name ?? "";
        if (!controller.Contains("PolicyPacksController", StringComparison.Ordinal))
            return;

        string block = "\n\n### Example request body (JSON)\n\n```json\n" + example.Trim() + "\n```\n";
        string intro =
            "**Policy pack content** (`initialContentJson` / `contentJson`) is JSON matching `PolicyPackContentDocument`: "
            + "`complianceRuleIds`, `complianceRuleKeys`, `alertRuleIds`, `compositeAlertRuleIds`, `advisoryDefaults`, `metadata`. "
            + "See `docs/API_CONTRACTS.md` § Policy packs.\n\n";

        operation.Description = string.IsNullOrWhiteSpace(operation.Description)
            ? intro + block
            : operation.Description.TrimEnd() + "\n\n" + intro + block;
    }
}
