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

    /// <summary>Maximum cached responses (memory cache entry count with uniform entry size).</summary>
    public int MaxEntries { get; set; } = 256;

    /// <summary>Time-to-live for a cached assistant message body.</summary>
    public int AbsoluteExpirationSeconds { get; set; } = 600;

    /// <summary>
    /// When true, the cache key includes tenant, workspace, and project from <see cref="ArchiForge.Core.Scoping.IScopeContextProvider"/>.
    /// Prevents cross-tenant reuse when prompts are identical.
    /// </summary>
    public bool PartitionByScope { get; set; } = true;
}
