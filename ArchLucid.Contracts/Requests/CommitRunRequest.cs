namespace ArchLucid.Contracts.Requests;

/// <summary>Optional body for POST <c>/v1/architecture/run/{{runId}}/commit</c>.</summary>
public sealed class CommitRunRequest
{
    /// <summary>
    ///     When <see langword="true" />, sends a transactional email to the tenant provisioned admin contact
    ///     (when resolvable and email is configured) with a link to the run in the operator UI.
    /// </summary>
    public bool NotifySponsor
    {
        get;
        init;
    }
}
