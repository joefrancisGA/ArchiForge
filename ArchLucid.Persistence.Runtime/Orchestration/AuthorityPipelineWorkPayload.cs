using ArchLucid.ContextIngestion.Models;

namespace ArchLucid.Persistence.Orchestration;

/// <summary>
///     JSON payload stored in <c>dbo.AuthorityPipelineWorkOutbox</c> for deferred authority continuation.
/// </summary>
public sealed class AuthorityPipelineWorkPayload
{
    public ContextIngestionRequest ContextIngestionRequest
    {
        get;
        set;
    } = null!;

    /// <summary>
    ///     Evidence bundle id persisted during deferred create before the worker completes.
    /// </summary>
    public string EvidenceBundleId
    {
        get;
        set;
    } = "";
}
