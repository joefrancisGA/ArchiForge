namespace ArchLucid.Retrieval.Indexing;

/// <summary>
///     Caps embedding cost and request size during <see cref="IRetrievalIndexingService" /> runs (batch size and total
///     chunks per operation).
/// </summary>
public sealed class RetrievalEmbeddingCapOptions
{
    public const string SectionName = "Retrieval:EmbeddingCaps";

    /// <summary>
    ///     Maximum texts sent per <see cref="Embedding.IEmbeddingService.EmbedManyAsync" /> call; Azure OpenAI enforces
    ///     its own limits — keep this conservative.
    /// </summary>
    public int MaxTextsPerEmbeddingRequest
    {
        get;
        set;
    } = 16;

    /// <summary>
    ///     Maximum total chunks embedded in one <see cref="IRetrievalIndexingService.IndexDocumentsAsync" /> call; 0 =
    ///     unlimited.
    /// </summary>
    public int MaxChunksPerIndexOperation
    {
        get;
        set;
    } = 0;
}
