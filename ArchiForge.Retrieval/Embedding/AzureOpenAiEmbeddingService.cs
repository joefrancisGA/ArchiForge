using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Retrieval.Embedding;

/// <summary>
/// <see cref="IEmbeddingService"/> adapter over <see cref="IOpenAiEmbeddingClient"/> for DI composition.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Passthrough adapter; all logic lives in IOpenAiEmbeddingClient which is tested via its interface.")]
public sealed class AzureOpenAiEmbeddingService(IOpenAiEmbeddingClient client) : IEmbeddingService
{
    /// <inheritdoc />
    public Task<float[]> EmbedAsync(string text, CancellationToken ct) =>
        client.EmbedAsync(text, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<float[]>> EmbedManyAsync(IReadOnlyList<string> texts, CancellationToken ct) =>
        client.EmbedManyAsync(texts, ct);
}
