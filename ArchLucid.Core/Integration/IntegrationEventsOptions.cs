namespace ArchLucid.Core.Integration;

/// <summary>Optional Azure Service Bus publishing and transactional outbox for integration events.</summary>
public sealed class IntegrationEventsOptions
{
    public const string SectionName = "IntegrationEvents";

    /// <summary>When non-empty and <see cref="QueueOrTopicName" /> is set, publishing uses this connection string (legacy).</summary>
    public string? ServiceBusConnectionString
    {
        get;
        set;
    }

    /// <summary>
    ///     Fully qualified Service Bus namespace (e.g. <c>mysb.servicebus.windows.net</c>).
    ///     When set with <see cref="QueueOrTopicName" />, the client uses <c>DefaultAzureCredential</c> (managed identity in
    ///     Azure).
    /// </summary>
    public string? ServiceBusFullyQualifiedNamespace
    {
        get;
        set;
    }

    /// <summary>Optional user-assigned managed identity client id when using namespace + DefaultAzureCredential.</summary>
    public string? ServiceBusManagedIdentityClientId
    {
        get;
        set;
    }

    /// <summary>Queue or topic name for outbound integration JSON messages.</summary>
    public string? QueueOrTopicName
    {
        get;
        set;
    }

    /// <summary>
    ///     When true and the authority UOW supports an external SQL transaction, run-completed events are written to
    ///     <c>dbo.IntegrationEventOutbox</c> in the same transaction as the run commit; a worker publishes afterward.
    /// </summary>
    public bool TransactionalOutboxEnabled
    {
        get;
        set;
    }

    /// <summary>Total publish attempts per outbox row before it is dead-lettered (initial try + retries).</summary>
    public int OutboxMaxPublishAttempts
    {
        get;
        set;
    } = 6;

    /// <summary>Upper bound for exponential backoff delay between publish retries (seconds).</summary>
    public int OutboxMaxBackoffSeconds
    {
        get;
        set;
    } = 300;

    /// <summary>When true, the worker hosts a Service Bus subscription processor for integration events.</summary>
    public bool ConsumerEnabled
    {
        get;
        set;
    }

    /// <summary>Service Bus subscription under <see cref="QueueOrTopicName" /> (topic mode).</summary>
    public string? SubscriptionName
    {
        get;
        set;
    }

    /// <summary>Concurrent callbacks per processor instance.</summary>
    public int MaxConcurrentCalls
    {
        get;
        set;
    } = 4;

    /// <summary>Prefetch count for the processor (0 = SDK default).</summary>
    public int PrefetchCount
    {
        get;
        set;
    }
}
