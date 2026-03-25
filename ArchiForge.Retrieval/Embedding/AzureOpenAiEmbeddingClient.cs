using System.ClientModel;

using Azure.AI.OpenAI;

using OpenAI.Embeddings;

namespace ArchiForge.Retrieval.Embedding;

/// <summary>
/// Azure OpenAI text embeddings for a named embedding deployment on the resource.
/// </summary>
/// <remarks>Uses synchronous SDK calls wrapped in <see cref="Task"/>; suitable for app startup registration as singleton.</remarks>
public sealed class AzureOpenAiEmbeddingClient : IOpenAiEmbeddingClient
{
    private readonly EmbeddingClient _embeddingClient;

    /// <param name="endpoint">Azure OpenAI endpoint URI.</param>
    /// <param name="apiKey">API key credential.</param>
    /// <param name="embeddingDeploymentName">Embeddings deployment name (not the chat deployment).</param>
    public AzureOpenAiEmbeddingClient(string endpoint, string apiKey, string embeddingDeploymentName)
    {
        AzureOpenAIClient azureClient = new(new Uri(endpoint), new ApiKeyCredential(apiKey));
        _embeddingClient = azureClient.GetEmbeddingClient(embeddingDeploymentName);
    }

    public Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        _ = ct;
        ClientResult<OpenAIEmbedding>? result = _embeddingClient.GenerateEmbedding(text, cancellationToken: ct);
        return Task.FromResult(result.Value.ToFloats().ToArray());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<float[]>> EmbedManyAsync(IReadOnlyList<string> texts, CancellationToken ct)
    {
        _ = ct;
        if (texts.Count == 0)
            return Task.FromResult<IReadOnlyList<float[]>>([]);

        ClientResult<OpenAIEmbeddingCollection>? response = _embeddingClient.GenerateEmbeddings(texts.ToList(), cancellationToken: ct);
        List<float[]> vectors = response.Value.Select(e => e.ToFloats().ToArray()).ToList();
        return Task.FromResult<IReadOnlyList<float[]>>(vectors);
    }
}
