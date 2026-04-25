using ArchLucid.AgentRuntime.Prompts;
using ArchLucid.Core.Configuration;

using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime.Tests;

/// <summary>Builds a real <see cref="CachedAgentSystemPromptCatalog" /> for handler tests.</summary>
internal static class AgentPromptCatalogTestFactory
{
    public static IAgentSystemPromptCatalog Create(AgentPromptCatalogOptions? options = null)
    {
        AgentPromptCatalogOptions value = options ?? new AgentPromptCatalogOptions();

        return new CachedAgentSystemPromptCatalog(new StaticPromptOptionsMonitor(value));
    }

    private sealed class StaticPromptOptionsMonitor(AgentPromptCatalogOptions value)
        : IOptionsMonitor<AgentPromptCatalogOptions>
    {
        public AgentPromptCatalogOptions CurrentValue
        {
            get;
        } = value;

        public AgentPromptCatalogOptions Get(string? name)
        {
            return CurrentValue;
        }

        public IDisposable? OnChange(Action<AgentPromptCatalogOptions, string?> listener)
        {
            return null;
        }
    }
}
