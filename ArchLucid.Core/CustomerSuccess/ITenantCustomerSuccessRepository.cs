namespace ArchLucid.Core.CustomerSuccess;

/// <summary>Customer health scores and product feedback persistence.</summary>
public interface ITenantCustomerSuccessRepository
{
    Task<TenantHealthScoreRecord?> GetHealthScoreAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);

    Task InsertProductFeedbackAsync(ProductFeedbackSubmission submission, CancellationToken ct);

    /// <summary>
    ///     Recomputes and upserts health scores for all tenants (leader-elected worker; uses RLS bypass ambient).
    /// </summary>
    Task RefreshAllTenantHealthScoresAsync(CancellationToken ct);
}
