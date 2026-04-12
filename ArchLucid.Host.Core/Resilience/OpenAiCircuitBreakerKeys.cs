namespace ArchLucid.Host.Core.Resilience;

/// <summary>Keyed DI identifiers for independent Azure OpenAI circuit breakers (completion vs embeddings).</summary>
public static class OpenAiCircuitBreakerKeys
{
    public const string Completion = "OpenAiCompletion";

    /// <summary>Separate breaker for fallback chat completion when <c>ArchLucid:FallbackLlm</c> is enabled.</summary>
    public const string CompletionFallback = "OpenAiCompletionFallback";

    public const string Embedding = "OpenAiEmbedding";
}
