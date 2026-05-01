namespace ArchLucid.Core.Configuration;

/// <summary>One operator-facing row in <c>CONFIGURATION_REFERENCE.md</c> and the machine catalog.</summary>
public sealed record ConfigurationKeyEntry(
    string Section,
    string ConfigPath,
    IReadOnlyList<string> ConfigurationSources,
    string? Default,
    string RequiredSummary,
    string Description,
    ConfigKeyRequirementKind Requirement);

public enum ArchLucidConfigHostingRole
{
    Api,
    Worker,
    Combined,

    /// <summary>Applies to every process role (CLI, tests, any host).</summary>
    All
}
