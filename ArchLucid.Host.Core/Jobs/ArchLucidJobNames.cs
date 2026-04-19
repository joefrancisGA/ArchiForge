namespace ArchLucid.Host.Core.Jobs;

/// <summary>Canonical job slugs shared by configuration, Terraform, and CLI.</summary>
public static class ArchLucidJobNames
{
    public const string AdvisoryScan = "advisory-scan";

    public const string OrphanProbe = "orphan-probe";

    public const string DataArchival = "data-archival";

    public const string TrialLifecycle = "trial-lifecycle";

    public const string TrialEmailScan = "trial-email-scan";

    public const string AuditChangeFeed = "audit-change-feed";

    public const string ServiceBusIntegrationEvents = "servicebus-integration-events";

    /// <summary>
    /// In-process only today: <see cref="ArchLucid.Core.Audit.InMemoryAuditRetryQueue"/> is not shared across containers.
    /// Defer Container Apps offload until a durable queue exists (ADR 0018).
    /// </summary>
    public const string AuditRetryDrain = "audit-retry-drain";
}
