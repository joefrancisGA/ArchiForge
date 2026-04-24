namespace ArchLucid.Persistence.Orchestration;

/// <summary>One pending row in <c>dbo.AuthorityPipelineWorkOutbox</c>.</summary>
public sealed class AuthorityPipelineWorkOutboxEntry
{
    public Guid OutboxId
    {
        get;
        init;
    }

    public Guid RunId
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

    public string PayloadJson
    {
        get;
        init;
    } = "";

    public DateTime CreatedUtc
    {
        get;
        init;
    }
}
