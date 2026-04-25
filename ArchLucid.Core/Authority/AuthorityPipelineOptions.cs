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
}
