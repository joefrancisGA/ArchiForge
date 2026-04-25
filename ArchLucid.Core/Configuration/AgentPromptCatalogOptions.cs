namespace ArchLucid.Core.Configuration;

/// <summary>
///     Configuration for optional per-agent <strong>release</strong> labels (A/B, pilot) layered on built-in prompt
///     templates.
///     Semantic template versions and SHA-256 content hashes are defined alongside templates in the AgentRuntime assembly.
/// </summary>
public sealed class AgentPromptCatalogOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "AgentPrompts";

    /// <summary>
    ///     Per <c>agentTypeKey</c> (<c>topology</c>, <c>compliance</c>, …) release label stored on traces and OTel (e.g.
    ///     <c>experiment-a</c>).
    /// </summary>
    public Dictionary<string, string> Versions
    {
        get;
        set;
    } = new(StringComparer.OrdinalIgnoreCase);
}
