namespace ArchLucid.Provenance;

/// <summary>Persisted append-only snapshot of <see cref="DecisionProvenanceGraph" /> (JSON).</summary>
public class DecisionProvenanceSnapshot
{
    public Guid Id
    {
        get;
        set;
    }

    public Guid TenantId
    {
        get;
        set;
    }

    public Guid WorkspaceId
    {
        get;
        set;
    }

    public Guid ProjectId
    {
        get;
        set;
    }

    public Guid RunId
    {
        get;
        set;
    }

    public string GraphJson
    {
        get;
        set;
    } = "{}";

    public DateTime CreatedUtc
    {
        get;
        set;
    }
}
