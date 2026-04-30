using System.Diagnostics.CodeAnalysis;

namespace ArchLucid.AgentRuntime.Caching;

/// <summary>Options for <see cref="CachingLlmCompletionClient" /> (nested under configuration path <see cref="SectionName" />).</summary>
[ExcludeFromCodeCoverage(Justification = "Configuration binding DTO with no logic.")]
public sealed class LlmCompletionCacheOptions
{
    public const string SectionName = "AgentRuntime:CompletionCache";

    /// <summary>When false, the decorator forwards every call to the inner client.</summary>
    public bool Enabled
    {
        get; set;
    }

    /// <summary>Maximum cached entries (uniform size budget for the dedicated memory cache).</summary>
    public int MaxEntries
    {
        get;
        set;
    } = 1000;

    /// <summary>Default absolute expiration in minutes when <c>TTLSeconds</c> is unset (0 or negative).</summary>
    public int TTLMinutes
    {
        get;
        set;
    } = 30;

    /// <summary>
    ///     When greater than zero, overrides <see cref="TTLMinutes" /> (expiration is exactly this many seconds).
    /// </summary>
    public int TTLSeconds
    {
        get;
        set;
    }

    /// <summary>
    ///     When true, the cache key includes tenant, workspace, and project IDs from current scope (<see cref="ArchLucid.Core.Scoping.IScopeContextProvider" />).
    /// </summary>
    public bool PartitionByScope
    {
        get;
        set;
    } = true;
}
