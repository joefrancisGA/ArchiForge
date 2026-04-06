using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class CreateGovernanceEnvironmentComparisonRequest
{
    public string SourceEnvironment { get; set; } = "dev";
    public string TargetEnvironment { get; set; } = "test";
}
