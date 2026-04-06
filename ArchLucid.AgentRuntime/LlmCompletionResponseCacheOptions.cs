using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.AgentRuntime;

/// <summary>
/// In-process cache for identical chat completion requests (same prompts and scope partition).
/// </summary>
/// <remarks>
/// Reduces duplicate Azure OpenAI spend and latency when agents or Ask paths repeat the same prompt+context.
/// Entries are bounded by <see cref="MaxEntries"/>; enable/disable without redeploying via configuration.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Configuration binding DTO with no logic.")]
public sealed class LlmCompletionResponseCacheOptions
{
    public const string SectionName = "LlmCompletionCache";

    /// <summary>When false, completions always call the inner client.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary><c>Memory</c> (default) or <c>Distributed</c> (uses distributed cache / Redis).</summary>
    public string Provider { get; set; } = "Memory";

    /// <summary>Optional Redis connection when <see cref="Provider"/> is <c>Distributed</c> and no shared <c>IDistributedCache</c> is registered yet.</summary>
    /// <remarks>Falls back to <c>HotPathCache:RedisConnectionString</c> when unset.</remarks>
    public string? RedisConnectionString { get; set; }

    /// <summary>Maximum cached responses for <c>Memory</c> provider (entry count with uniform entry size).</summary>
    public int MaxEntries { get; set; } = 256;

    /// <summary>Time-to-live for a cached assistant message body.</summary>
    public int AbsoluteExpirationSeconds { get; set; } = 600;

    /// <summary>
    /// When true, the cache key includes tenant, workspace, and project from <see cref="ArchiForge.Core.Scoping.IScopeContextProvider"/>.
    /// Prevents cross-tenant reuse when prompts are identical.
    /// </summary>
    public bool PartitionByScope { get; set; } = true;
}
