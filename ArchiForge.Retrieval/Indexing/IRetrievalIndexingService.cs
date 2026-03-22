using ArchiForge.Retrieval.Models;

namespace ArchiForge.Retrieval.Indexing;

public interface IRetrievalIndexingService
{
    Task IndexDocumentsAsync(IReadOnlyList<RetrievalDocument> documents, CancellationToken ct);
}
