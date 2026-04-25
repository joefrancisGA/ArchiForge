namespace ArchLucid.Core.CustomerSuccess;

/// <summary>Thumbs feedback on a finding (or run-level) for PMF instrumentation.</summary>
public sealed class ProductFeedbackSubmission
{
    public Guid TenantId
    {
        get;
        init;
    }

    public Guid WorkspaceId
    {
        get;
        init;
    }

    public Guid ProjectId
    {
        get;
        init;
    }

    public string? FindingRef
    {
        get;
        init;
    }

    public Guid? RunId
    {
        get;
        init;
    }

    /// <summary>-1 = thumbs down, 0 = neutral, 1 = thumbs up.</summary>
    public short Score
    {
        get;
        init;
    }

    public string? Comment
    {
        get;
        init;
    }
}
