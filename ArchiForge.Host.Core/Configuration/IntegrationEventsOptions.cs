namespace ArchiForge.Host.Core.Configuration;

/// <summary>Optional Azure Service Bus publishing for integration events (e.g. authority run completed).</summary>
public sealed class IntegrationEventsOptions
{
    public const string SectionName = "IntegrationEvents";

    /// <summary>When non-empty and <see cref="QueueOrTopicName"/> is set, Azure Service Bus publishing is enabled.</summary>
    public string? ServiceBusConnectionString { get; set; }

    /// <summary>Queue or topic name for outbound integration JSON messages.</summary>
    public string? QueueOrTopicName { get; set; }
}
