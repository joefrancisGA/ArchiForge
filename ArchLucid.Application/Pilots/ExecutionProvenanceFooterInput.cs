namespace ArchLucid.Application.Pilots;
/// <summary>Inputs for the first-value report execution provenance footer (pilot try --real path).</summary>
public sealed record ExecutionProvenanceFooterInput(bool RealModeFellBackToSimulator, string? PilotAoaiDeploymentSnapshot, string HostAgentExecutionMode, string? HostAzureOpenAiDeploymentName, int LlmCompletionTraceCount)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(PilotAoaiDeploymentSnapshot, HostAgentExecutionMode, HostAzureOpenAiDeploymentName);
    private static byte __ValidatePrimaryConstructorArguments(System.String? PilotAoaiDeploymentSnapshot, System.String HostAgentExecutionMode, System.String? HostAzureOpenAiDeploymentName)
    {
        ArgumentNullException.ThrowIfNull(HostAgentExecutionMode);
        return (byte)0;
    }
}