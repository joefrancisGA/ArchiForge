namespace ArchiForge.Api.Services.Admin;

/// <summary>Pending outbox depths for operator dashboards.</summary>
public sealed record AdminOutboxSnapshot(
    long AuthorityPipelineWorkPending,
    long RetrievalIndexingPending,
    long IntegrationEventOutboxPublishPending,
    long IntegrationEventOutboxDeadLetter);
