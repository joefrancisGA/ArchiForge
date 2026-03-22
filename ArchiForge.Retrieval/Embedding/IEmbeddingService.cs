namespace ArchiForge.Retrieval.Embedding;

public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken ct);

    Task<IReadOnlyList<float[]>> EmbedManyAsync(IReadOnlyList<string> texts, CancellationToken ct);
}
