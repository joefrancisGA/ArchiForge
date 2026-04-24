namespace ArchLucid.Application.Pilots;

/// <summary>Inputs for the first-value report execution provenance footer (pilot try --real path).</summary>
public sealed record ExecutionProvenanceFooterInput(
    bool RealModeFellBackToSimulator,
    string? PilotAoaiDeploymentSnapshot,
    string HostAgentExecutionMode,
    string? HostAzureOpenAiDeploymentName,
    int LlmCompletionTraceCount);
