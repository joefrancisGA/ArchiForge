using ArchLucid.Retrieval.Indexing;

namespace ArchLucid.Host.Core.Startup.Validation.Rules;

internal static class RetrievalRules
{
    public static void CollectEmbeddingCaps(IConfiguration configuration, List<string> errors)
    {
        RetrievalEmbeddingCapOptions caps =
            configuration.GetSection(RetrievalEmbeddingCapOptions.SectionName).Get<RetrievalEmbeddingCapOptions>() ??
            new RetrievalEmbeddingCapOptions();

        if (caps.MaxTextsPerEmbeddingRequest is < 1 or > 2048)

            errors.Add("Retrieval:EmbeddingCaps:MaxTextsPerEmbeddingRequest must be between 1 and 2048.");

        if (caps.MaxChunksPerIndexOperation is < 0 or > 1_000_000)

            errors.Add("Retrieval:EmbeddingCaps:MaxChunksPerIndexOperation must be between 0 and 1000000 (0 = unlimited).");
    }

    /// <summary>
    /// Aligns with host composition retrieval registration: only InMemory and AzureSearch are supported; omitted defaults to in-memory.
    /// </summary>
    public static void CollectVectorIndex(IConfiguration configuration, List<string> errors)
    {
        string? mode = configuration["Retrieval:VectorIndex"];

        if (string.IsNullOrWhiteSpace(mode))
            return;

        if (string.Equals(mode, "InMemory", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mode, "AzureSearch", StringComparison.OrdinalIgnoreCase))

            return;

        errors.Add(
            "Retrieval:VectorIndex must be 'InMemory', 'AzureSearch', or omitted (defaults to InMemory).");
    }
}
