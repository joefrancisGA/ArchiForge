namespace ArchLucid.Contracts.Findings;

/// <summary>Durable review-trail row for operator audits (SQL-backed in production composition).</summary>
public sealed class FindingReviewEventRecord
{
    public Guid EventId
    {
        get;
        init;
    }

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

    public string FindingId
    {
        get;
        init;
    } = string.Empty;

    public string ReviewerUserId
    {
        get;
        init;
    } = string.Empty;

    public FindingReviewAction Action
    {
        get;
        init;
    }

    public string? Notes
    {
        get;
        init;
    }

    public DateTimeOffset OccurredAtUtc
    {
        get;
        init;
    }

    public Guid? RunId
    {
        get;
        init;
    }
}
