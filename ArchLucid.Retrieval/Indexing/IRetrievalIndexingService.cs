using ArchLucid.Retrieval.Models;

namespace ArchLucid.Retrieval.Indexing;

/// <summary>
///     Chunks <see cref="RetrievalDocument" /> content, embeds chunks, and upserts them into <see cref="IVectorIndex" />.
/// </summary>
/// <remarks>
///     Implementation: <see cref="RetrievalIndexingService" />. Used after run completion and by <c>AskService</c>
///     for conversation turns.
/// </remarks>
public interface IRetrievalIndexingService
{
    /// <summary>
    ///     No-op when <paramref name="documents" /> is empty. Throws if embedding batch size does not match chunk count for
    ///     any document.
    /// </summary>
    Task IndexDocumentsAsync(IReadOnlyList<RetrievalDocument> documents, CancellationToken ct);
}
