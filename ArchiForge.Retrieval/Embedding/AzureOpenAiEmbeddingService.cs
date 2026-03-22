namespace ArchiForge.Retrieval.Embedding;

public sealed class AzureOpenAiEmbeddingService(IOpenAiEmbeddingClient client) : IEmbeddingService
{
    public Task<float[]> EmbedAsync(string text, CancellationToken ct) =>
        client.EmbedAsync(text, ct);

    public Task<IReadOnlyList<float[]>> EmbedManyAsync(IReadOnlyList<string> texts, CancellationToken ct) =>
        client.EmbedManyAsync(texts, ct);
}
