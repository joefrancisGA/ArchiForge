using System.ClientModel;
using Azure.AI.OpenAI;
using OpenAI.Embeddings;

namespace ArchiForge.Retrieval.Embedding;

/// <summary>Azure OpenAI text embeddings using the shared endpoint/key.</summary>
public sealed class AzureOpenAiEmbeddingClient : IOpenAiEmbeddingClient
{
    private readonly EmbeddingClient _embeddingClient;

    public AzureOpenAiEmbeddingClient(string endpoint, string apiKey, string embeddingDeploymentName)
    {
        var azureClient = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));
        _embeddingClient = azureClient.GetEmbeddingClient(embeddingDeploymentName);
    }

    public Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        _ = ct;
        var result = _embeddingClient.GenerateEmbedding(text, cancellationToken: ct);
        return Task.FromResult(result.Value.ToFloats().ToArray());
    }

    public Task<IReadOnlyList<float[]>> EmbedManyAsync(IReadOnlyList<string> texts, CancellationToken ct)
    {
        _ = ct;
        if (texts.Count == 0)
            return Task.FromResult<IReadOnlyList<float[]>>([]);

        var response = _embeddingClient.GenerateEmbeddings(texts.ToList(), cancellationToken: ct);
        var vectors = response.Value.Select(e => e.ToFloats().ToArray()).ToList();
        return Task.FromResult<IReadOnlyList<float[]>>(vectors);
    }
}
