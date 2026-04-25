namespace ArchLucid.Persistence.Models;

/// <summary>
///     A single committed authority run row used to resolve the latest publishable reference evidence for a tenant.
/// </summary>
public sealed class ReferenceEvidenceRunCandidate
{
    public Guid RunId
    {
        get;
        init;
    }

    public Guid WorkspaceId
    {
        get;
        init;
    }

    public Guid ScopeProjectId
    {
        get;
        init;
    }

    /// <summary>ArchitectureRequests.RequestId when present; used for demo-prefix detection.</summary>
    public string? RequestId
    {
        get;
        init;
    }
}
