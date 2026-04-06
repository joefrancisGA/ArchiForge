using ArchiForge.Retrieval.Models;

namespace ArchiForge.Retrieval.Queries;

/// <summary>
/// High-level semantic search over indexed retrieval chunks for a scoped query.
/// </summary>
/// <remarks>
/// Implementation: <see cref="RetrievalQueryService"/>. Callers: <c>ArchiForge.Api.Services.Ask.AskService</c>, <c>ArchiForge.Api.Controllers.RetrievalController</c>.
/// </remarks>
public interface IRetrievalQueryService
{
    /// <summary>
    /// Embeds <see cref="RetrievalQuery.QueryText"/> then queries <see cref="Indexing.IVectorIndex"/>.
    /// </summary>
    /// <param name="query">Scope, optional run/manifest filters, text, and <see cref="RetrievalQuery.TopK"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Ranked hits; may be empty when nothing is indexed or filters exclude all chunks.</returns>
    Task<IReadOnlyList<RetrievalHit>> SearchAsync(RetrievalQuery query, CancellationToken ct);
}
