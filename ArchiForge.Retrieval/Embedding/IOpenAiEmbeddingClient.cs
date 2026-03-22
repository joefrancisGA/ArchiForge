namespace ArchiForge.Retrieval.Embedding;

/// <summary>Low-level Azure OpenAI embeddings seam (deployment-specific).</summary>
public interface IOpenAiEmbeddingClient
{
    Task<float[]> EmbedAsync(string text, CancellationToken ct);

    Task<IReadOnlyList<float[]>> EmbedManyAsync(IReadOnlyList<string> texts, CancellationToken ct);
}
