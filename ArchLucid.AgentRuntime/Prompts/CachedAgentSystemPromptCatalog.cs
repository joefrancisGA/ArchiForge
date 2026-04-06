using ArchLucid.Contracts.Common;
using ArchLucid.Core.Configuration;

using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime.Prompts;

/// <summary>
/// Loads built-in templates once, precomputes content hashes, and layers optional <see cref="AgentPromptCatalogOptions.Versions"/> release labels.
/// </summary>
public sealed class CachedAgentSystemPromptCatalog(IOptionsMonitor<AgentPromptCatalogOptions> optionsMonitor)
    : IAgentSystemPromptCatalog
{
    private readonly IOptionsMonitor<AgentPromptCatalogOptions> _optionsMonitor =
        optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));

    private static readonly IReadOnlyDictionary<AgentType, PromptTemplateCore> Templates = BuildTemplates();

    /// <inheritdoc />
    public ResolvedSystemPrompt Resolve(AgentType agentType)
    {
        if (!Templates.TryGetValue(agentType, out PromptTemplateCore? core) || core is null)
        {
            throw new InvalidOperationException(
                $"No system prompt template is registered for agent type '{agentType}'.");
        }

        string dispatchKey = AgentTypeKeys.FromEnum(agentType);
        string? release = null;
        AgentPromptCatalogOptions opts = _optionsMonitor.CurrentValue;

        if (opts.Versions.TryGetValue(dispatchKey, out string? configured) && !string.IsNullOrWhiteSpace(configured))
        {
            release = configured.Trim();
        }

        return new ResolvedSystemPrompt(core.Text, core.TemplateId, core.TemplateVersion, core.ContentSha256Hex, release);
    }

    private sealed record PromptTemplateCore(
        string Text,
        string TemplateId,
        string TemplateVersion,
        string ContentSha256Hex);

    private static IReadOnlyDictionary<AgentType, PromptTemplateCore> BuildTemplates()
    {
        Dictionary<AgentType, PromptTemplateCore> map = new()
        {
            [AgentType.Topology] = CreateCore(
                TopologySystemPromptTemplate.TemplateId,
                TopologySystemPromptTemplate.Version,
                TopologySystemPromptTemplate.GetText),
            [AgentType.Compliance] = CreateCore(
                ComplianceSystemPromptTemplate.TemplateId,
                ComplianceSystemPromptTemplate.Version,
                ComplianceSystemPromptTemplate.GetText),
            [AgentType.Critic] = CreateCore(
                CriticSystemPromptTemplate.TemplateId,
                CriticSystemPromptTemplate.Version,
                CriticSystemPromptTemplate.GetText),
        };

        return map;
    }

    private static PromptTemplateCore CreateCore(string templateId, string templateVersion, Func<string> getText)
    {
        string text = getText();
        string hash = AgentPromptCanonicalHasher.Sha256HexUtf8Normalized(text);

        return new PromptTemplateCore(text, templateId, templateVersion, hash);
    }
}
