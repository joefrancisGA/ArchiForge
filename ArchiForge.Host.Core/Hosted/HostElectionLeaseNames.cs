namespace ArchiForge.Host.Core.Hosted;

/// <summary>
/// Keys in <c>dbo.HostLeaderLeases</c>; must stay stable across versions.
/// </summary>
public static class HostElectionLeaseNames
{
    public const string AdvisoryScanPolling = "hosted:advisory-scan-polling";

    public const string DataArchival = "hosted:data-archival";

    public const string RetrievalIndexingOutbox = "hosted:retrieval-indexing-outbox";
}
