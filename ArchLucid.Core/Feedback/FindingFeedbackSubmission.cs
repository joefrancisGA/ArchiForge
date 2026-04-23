namespace ArchLucid.Core.Feedback;

/// <summary>Append-only thumbs vote for a single finding on an architecture run (tenant-scoped).</summary>
public sealed class FindingFeedbackSubmission
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

    public Guid RunId
    {
        get;
        init;
    }

    public string FindingId
    {
        get;
        init;
    } = string.Empty;

    /// <summary>-1 (thumbs down) or +1 (thumbs up).</summary>
    public short Score
    {
        get;
        init;
    }
}
