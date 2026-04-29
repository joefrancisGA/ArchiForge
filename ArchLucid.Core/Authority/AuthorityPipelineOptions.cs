namespace ArchLucid.Core.Authority;

/// <summary>
///     Time bounds for synchronous authority pipeline execution (or queued completion) after the run row is persisted.
/// </summary>
public sealed class AuthorityPipelineOptions
{
    public const string SectionName = "AuthorityPipeline";

    /// <summary>
    ///     Cancels the pipeline token after this duration. Use <see cref="TimeSpan.Zero" /> to disable (no timeout).
    /// </summary>
    public TimeSpan PipelineTimeout
    {
        get;
        set;
    } = TimeSpan.FromMinutes(5);

    /// <summary>
    ///     When <see langword="true" /> (default), abort the authority pipeline before decisioning if the findings
    ///     snapshot is only <see cref="ArchLucid.Contracts.Findings.FindingsSnapshotGenerationStatus.PartiallyComplete" />
    ///     (some finding engines failed). When <see langword="false" />, decisioning proceeds with available findings
    ///     and operators rely on <see cref="ArchLucid.Decisioning.Models.FindingsSnapshot.EngineFailures" /> telemetry.
    /// </summary>
    public bool HaltOnPartialFindings
    {
        get;
        set;
    } = true;
}
