namespace ArchLucid.Host.Core.Hosted;

/// <summary>
/// Keys in <c>dbo.HostLeaderLeases</c>; must stay stable across versions.
/// </summary>
public static class HostElectionLeaseNames
{
    public const string AdvisoryScanPolling = "hosted:advisory-scan-polling";

    public const string DataArchival = "hosted:data-archival";

    public const string RetrievalIndexingOutbox = "hosted:retrieval-indexing-outbox";

    public const string IntegrationEventOutbox = "hosted:integration-event-outbox";

    public const string AuthorityPipelineWorkOutbox = "hosted:authority-pipeline-work-outbox";

    public const string TrialLifecycleEmailPolling = "hosted:trial-lifecycle-email-polling";

    public const string ExecDigestWeeklyPolling = "hosted:exec-digest-weekly-polling";

    public const string TrialLifecycleAutomation = "hosted:trial-lifecycle-automation";

    public const string TenantHealthScoring = "hosted:tenant-health-scoring";

    public const string TrialArchitecturePreseed = "hosted:trial-architecture-preseed";
}
