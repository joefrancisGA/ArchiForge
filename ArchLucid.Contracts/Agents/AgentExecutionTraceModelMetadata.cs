namespace ArchLucid.Contracts.Agents;

/// <summary>
/// Canonical strings for <see cref="AgentExecutionTrace.ModelDeploymentName"/> and
/// <see cref="AgentExecutionTrace.ModelVersion"/> when the provider does not supply values,
/// so persisted traces remain queryable and log forging / null ambiguity is avoided for Real-mode rows.
/// </summary>
/// <remarks>
/// Simulator-mode traces should use explicit <see cref="SimulatorDeploymentName"/> /
/// <see cref="SimulatorModelVersion"/> from the recording executor instead of these sentinels.
/// </remarks>
public static class AgentExecutionTraceModelMetadata
{
    /// <summary>Placeholder when the completion client did not report a deployment name.</summary>
    public const string UnspecifiedDeploymentName = "unspecified-deployment";

    /// <summary>Placeholder when the completion client did not report a model version.</summary>
    public const string UnspecifiedModelVersion = "unspecified-model-version";

    /// <summary>Recorded for <c>AgentExecution:Mode=Simulator</c> traces (deterministic executor).</summary>
    public const string SimulatorDeploymentName = "AgentExecution:Simulator";

    /// <summary>Recorded for simulator traces; bumps if simulator output shape changes materially.</summary>
    public const string SimulatorModelVersion = "deterministic-1.0";
}
