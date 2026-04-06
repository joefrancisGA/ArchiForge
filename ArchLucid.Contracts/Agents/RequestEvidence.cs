namespace ArchiForge.Contracts.Agents;

/// <summary>
/// Summarises the original architecture request, distilled into the key inputs agents
/// need to reason about: the description, stated constraints, required capabilities, and assumptions.
/// </summary>
public sealed class RequestEvidence
{
    /// <summary>Plain-text description of the architecture request.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Non-negotiable constraints the solution must satisfy (e.g., region, budget, compliance framework).</summary>
    public List<string> Constraints { get; set; } = [];

    /// <summary>Functional or non-functional capabilities the solution must provide.</summary>
    public List<string> RequiredCapabilities { get; set; } = [];

    /// <summary>Assumptions baked into the request at submission time.</summary>
    public List<string> Assumptions { get; set; } = [];
}
