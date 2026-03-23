namespace ArchiForge.Decisioning.Advisory.Workflow;

/// <summary>
/// Persists <see cref="RecommendationRecord"/> rows scoped by tenant, workspace, project, and authority <see cref="RecommendationRecord.RunId"/>.
/// </summary>
/// <remarks>
/// SQL table: <c>dbo.RecommendationRecords</c>. Implementations live in <c>ArchiForge.Persistence.Advisory</c> (Dapper / in-memory).
/// HTTP: <c>ArchiForge.Api.Controllers.AdvisoryController</c> lists and drives actions; workflow persistence uses
/// <c>RecommendationWorkflowService</c>; learning and alert simulation also read counts via this abstraction.
/// </remarks>
public interface IRecommendationRepository
{
    /// <summary>Inserts or updates a single recommendation by <see cref="RecommendationRecord.RecommendationId"/>.</summary>
    /// <param name="recommendation">Fully populated row; JSON columns must already be serialized strings.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpsertAsync(RecommendationRecord recommendation, CancellationToken ct);

    /// <summary>Loads one recommendation by id (any scope).</summary>
    /// <returns>The row, or <c>null</c> if not found.</returns>
    Task<RecommendationRecord?> GetByIdAsync(Guid recommendationId, CancellationToken ct);

    /// <summary>Lists recommendations for a specific authority run within the given scope.</summary>
    /// <returns>Ordered by priority score descending, then <see cref="RecommendationRecord.CreatedUtc"/> descending.</returns>
    Task<IReadOnlyList<RecommendationRecord>> ListByRunAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid runId,
        CancellationToken ct);

    /// <summary>Lists the most recently updated recommendations in the scope, optionally filtered by status string.</summary>
    /// <param name="status">When non-null, only rows whose <see cref="RecommendationRecord.Status"/> equals this value.</param>
    /// <param name="take">Maximum rows to return (SQL Server: <c>TOP</c>).</param>
    /// <returns>Newest <see cref="RecommendationRecord.LastUpdatedUtc"/> first.</returns>
    Task<IReadOnlyList<RecommendationRecord>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string? status,
        int take,
        CancellationToken ct);
}
