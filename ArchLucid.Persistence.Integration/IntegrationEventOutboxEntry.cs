namespace ArchLucid.Persistence;

/// <summary>One row in <c>dbo.IntegrationEventOutbox</c> awaiting Service Bus publish.</summary>
public sealed class IntegrationEventOutboxEntry
{
    public Guid OutboxId
    {
        get; init;
    }

    public Guid? RunId
    {
        get; init;
    }

    public required string EventType
    {
        get; init;
    }

    public string? MessageId
    {
        get; init;
    }

    public required byte[] PayloadUtf8
    {
        get; init;
    }

    public Guid TenantId
    {
        get; init;
    }

    public Guid WorkspaceId
    {
        get; init;
    }

    public Guid ProjectId
    {
        get; init;
    }

    public DateTime CreatedUtc
    {
        get; init;
    }

    /// <summary>Dequeue tier (nullable for pre-migration rows; treat as standard when absent).</summary>
    public int? Priority
    {
        get; init;
    }

    public int RetryCount
    {
        get; init;
    }

    public DateTime? NextRetryUtc
    {
        get; init;
    }

    public string? LastErrorMessage
    {
        get; init;
    }

    public DateTime? DeadLetteredUtc
    {
        get; init;
    }
}
