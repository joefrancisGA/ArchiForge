using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace ArchiForge.Api.Swagger;

public sealed class ReplayExamplesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody?.Content == null)
            return;

        string path = context.ApiDescription.RelativePath ?? "";
        if (!path.Contains("comparisons", StringComparison.OrdinalIgnoreCase) || !path.Contains("replay", StringComparison.OrdinalIgnoreCase))
            return;

        operation.Summary ??= "Replay a persisted comparison";
        string baseDesc = "Replay examples: "
                          + "artifact-markdown: format=markdown, replayMode=artifact, persistReplay=false. "
                          + "verify-persist: format=markdown, replayMode=verify, profile=detailed, persistReplay=true. "
                          + "docx-executive: format=docx, replayMode=artifact, profile=executive, persistReplay=false. "
                          + "**Verify mode:** regenerates the comparison and diffs against the stored payload. "
                          + "If drift is detected, the API returns **422 Unprocessable Entity** (application/problem+json) "
                          + "with type `#comparison-verification-failed`, plus extensions **driftDetected** (boolean) and **driftSummary** (string) when applicable. "
                          + "Successful verify returns **200** with the replay artifact and header X-ArchiForge-VerificationPassed=true.";
        operation.Description = string.IsNullOrWhiteSpace(operation.Description)
            ? baseDesc
            : operation.Description + " " + baseDesc;

        operation.Responses?.TryAdd("422", new OpenApiResponse
        {
            Description =
                    "Comparison verification failed: regenerated output does not match the stored comparison payload. "
                    + "Problem details may include driftDetected and driftSummary."
        });
    }
}
