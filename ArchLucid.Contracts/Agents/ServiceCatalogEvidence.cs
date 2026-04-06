namespace ArchiForge.Contracts.Agents;

/// <summary>
/// Represents a single service-catalog entry included in an <see cref="AgentEvidencePackage"/>.
/// Agents use service-catalog evidence to select appropriate managed services when proposing
/// topology changes for the architecture.
/// </summary>
public sealed class ServiceCatalogEvidence
{
    /// <summary>Stable identifier for the catalog entry (e.g., <c>azure-service-bus</c>).</summary>
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>Human-readable display name of the service (e.g., <c>Azure Service Bus</c>).</summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Classification category grouping related services (e.g., <c>Messaging</c>, <c>Storage</c>,
    /// <c>Compute</c>). Agents may use this to filter relevant entries.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Brief description of what the service does and when to prefer it.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Classification tags for filtering (e.g., <c>managed</c>, <c>serverless</c>, <c>regional</c>).</summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Scenarios where this service is the preferred or canonical choice
    /// (e.g., <c>Async event fan-out</c>, <c>Durable message queuing</c>).
    /// </summary>
    public List<string> RecommendedUseCases { get; set; } = [];
}
