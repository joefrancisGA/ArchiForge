namespace ArchiForge.Persistence.Integration;

/// <summary>Admin-facing summary of a dead-lettered integration outbox row.</summary>
public sealed class IntegrationEventOutboxDeadLetterRow
{
    public Guid OutboxId { get; init; }

    public Guid? RunId { get; init; }

    public required string EventType { get; init; }

    public DateTime DeadLetteredUtc { get; init; }

    public int RetryCount { get; init; }

    public string? LastErrorMessage { get; init; }
}
