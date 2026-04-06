namespace ArchiForge.Contracts.Agents;

/// <summary>
/// Represents a pattern library entry included in an <see cref="AgentEvidencePackage"/>.
/// Agents use pattern evidence to select proven architectural approaches when addressing
/// the capabilities described in <see cref="RequestEvidence"/>.
/// </summary>
public sealed class PatternEvidence
{
    /// <summary>Stable identifier for the architectural pattern (e.g., <c>pattern-event-driven</c>).</summary>
    public string PatternId { get; set; } = string.Empty;

    /// <summary>Human-readable name of the pattern (e.g., <c>Event-Driven Architecture</c>).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Concise description of when and why to apply this pattern.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Required capabilities from <see cref="RequestEvidence.RequiredCapabilities"/> that
    /// this pattern directly addresses.
    /// </summary>
    public List<string> ApplicableCapabilities { get; set; } = [];

    /// <summary>
    /// Canonical service types or names typically used when implementing this pattern
    /// (e.g., <c>Azure Service Bus</c>, <c>Azure Event Grid</c>).
    /// </summary>
    public List<string> SuggestedServices { get; set; } = [];
}
