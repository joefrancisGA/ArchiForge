using ArchLucid.Core.Scoping;

namespace ArchLucid.KnowledgeGraph.Caching;

/// <summary>Stable cache keys for <see cref="Interfaces.IGraphSnapshotProjectionCache" />.</summary>
public static class GraphSnapshotProjectionCacheKeys
{
    private const string Prefix = "al:kgproj:";

    /// <summary>Scope + run + graph snapshot id (matches how <c>GetRunDetailAsync</c> resolves the graph leg).</summary>
    public static string Projection(ScopeContext scope, Guid runId, Guid graphSnapshotId)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return
            $"{Prefix}{scope.TenantId:N}:{scope.WorkspaceId:N}:{scope.ProjectId:N}:{runId:N}:{graphSnapshotId:N}";
    }
}
