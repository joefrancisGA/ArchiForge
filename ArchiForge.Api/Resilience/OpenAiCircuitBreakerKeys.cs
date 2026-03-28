namespace ArchiForge.Api.Resilience;

/// <summary>Keyed DI identifiers for independent Azure OpenAI circuit breakers (completion vs embeddings).</summary>
public static class OpenAiCircuitBreakerKeys
{
    public const string Completion = "OpenAiCompletion";

    public const string Embedding = "OpenAiEmbedding";
}
