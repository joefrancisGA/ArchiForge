namespace ArchLucid.Decisioning.Governance.PolicyPacks;

/// <summary>
///     Per-HTTP-request cache for <see cref="IEffectiveGovernanceLoader.LoadEffectiveContentAsync" /> so multiple
///     consumers
///     in the same scope (alerts, compliance, advisory paths) do not repeat full resolution work.
/// </summary>
/// <remarks>
///     Registered scoped: one instance per request. Cache key is the (tenant, workspace, project) triple passed to the
///     loader.
/// </remarks>
public sealed class RequestScopedCachingEffectiveGovernanceLoader(IEffectiveGovernanceLoader inner)
    : IEffectiveGovernanceLoader
{
    private PolicyPackContentDocument? _cached;
    private bool _hasCache;
    private Guid _projectId;
    private Guid _tenantId;
    private Guid _workspaceId;

    /// <inheritdoc />
    public async Task<PolicyPackContentDocument> LoadEffectiveContentAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        if (_hasCache &&
            _tenantId == tenantId &&
            _workspaceId == workspaceId &&
            _projectId == projectId &&
            _cached is not null)

            return _cached;

        PolicyPackContentDocument document = await inner
                .LoadEffectiveContentAsync(tenantId, workspaceId, projectId, ct)
            ;

        _hasCache = true;
        _tenantId = tenantId;
        _workspaceId = workspaceId;
        _projectId = projectId;
        _cached = document;

        return document;
    }
}
