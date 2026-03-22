using ArchiForge.Retrieval.Models;

namespace ArchiForge.Retrieval.Queries;

public interface IRetrievalQueryService
{
    Task<IReadOnlyList<RetrievalHit>> SearchAsync(RetrievalQuery query, CancellationToken ct);
}
