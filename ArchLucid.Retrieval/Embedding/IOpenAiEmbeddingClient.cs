namespace ArchiForge.Retrieval.Embedding;

/// <summary>
/// Low-level embeddings seam (typically Azure OpenAI <see cref="AzureOpenAiEmbeddingClient"/>). Consumed by <see cref="AzureOpenAiEmbeddingService"/>.
/// </summary>
public interface IOpenAiEmbeddingClient
{
    /// <summary>Single-text embedding vector.</summary>
    /// <param name="text">Plain text to embed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Float vector (dimension depends on the underlying model).</returns>
    Task<float[]> EmbedAsync(string text, CancellationToken ct);

    /// <summary>Batched embeddings in input order (empty list returns empty).</summary>
    /// <param name="texts">Texts to embed; may be empty.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Float vectors in the same order as <paramref name="texts"/>.</returns>
    Task<IReadOnlyList<float[]>> EmbedManyAsync(IReadOnlyList<string> texts, CancellationToken ct);
}
