namespace ArchiForge.Core.Ask;

/// <summary>Natural-language question against grounded run (and optional comparison) context.</summary>
public sealed class AskRequest
{
    /// <summary>Existing conversation thread; omit to start a new thread.</summary>
    public Guid? ThreadId { get; set; }

    /// <summary>Primary run whose GoldenManifest anchors the answer (optional when continuing a threaded conversation that already has a run).</summary>
    public Guid? RunId { get; set; }

    /// <summary>When set with <see cref="TargetRunId"/>, includes structured comparison in context.</summary>
    public Guid? BaseRunId { get; set; }

    public Guid? TargetRunId { get; set; }

    public string Question { get; set; } = "";
}
