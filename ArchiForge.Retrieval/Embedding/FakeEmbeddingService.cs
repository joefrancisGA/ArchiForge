namespace ArchiForge.Retrieval.Embedding;

/// <summary>Deterministic pseudo-vectors for local dev/tests (not semantically meaningful).</summary>
public sealed class FakeEmbeddingService : IEmbeddingService
{
    public Task<float[]> EmbedAsync(string text, CancellationToken ct) =>
        Task.FromResult(Build(text));

    public Task<IReadOnlyList<float[]>> EmbedManyAsync(IReadOnlyList<string> texts, CancellationToken ct)
    {
        IReadOnlyList<float[]> result = texts.Select(Build).ToList();
        return Task.FromResult(result);
    }

    private static float[] Build(string text)
    {
        var vector = new float[32];
        var hash = text.GetHashCode();

        for (var i = 0; i < vector.Length; i++)
            vector[i] = ((hash >> (i % 16)) & 255) / 255f;

        return vector;
    }
}
