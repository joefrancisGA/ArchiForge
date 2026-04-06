namespace ArchiForge.Core.Diagnostics;

/// <summary>Cached outbox depths for Prometheus observable gauges (updated by a background collector).</summary>
public readonly record struct OutboxDepthGaugeValues(
    long AuthorityPipelineWorkPending,
    double AuthorityPipelineWorkOldestPendingAgeSeconds,
    long RetrievalIndexingOutboxPending,
    double RetrievalIndexingOutboxOldestPendingAgeSeconds,
    long IntegrationEventOutboxPublishPending,
    long IntegrationEventOutboxDeadLetter,
    double IntegrationEventOutboxOldestActionablePendingAgeSeconds);
