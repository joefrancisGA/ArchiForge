using ArchiForge.Core.Scoping;

namespace ArchiForge.Persistence.Caching;

/// <summary>Stable cache key fragments for hot-path repository decorators (tenant-scoped where applicable).</summary>
public static class HotPathCacheKeys
{
    private const string Prefix = "af:hot:";

    /// <summary>Golden manifest by authority scope + manifest id.</summary>
    public static string Manifest(ScopeContext scope, Guid manifestId)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return $"{Prefix}hm:{scope.TenantId:N}:{scope.WorkspaceId:N}:{scope.ProjectId:N}:{manifestId:N}";
    }

    /// <summary>Authority run row by scope + run id (matches <c>dbo.Runs</c> scope columns).</summary>
    public static string Run(ScopeContext scope, Guid runId)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return $"{Prefix}run:{scope.TenantId:N}:{scope.WorkspaceId:N}:{scope.ProjectId:N}:{runId:N}";
    }

    /// <summary>Policy pack metadata by surrogate key.</summary>
    public static string PolicyPack(Guid policyPackId) => $"{Prefix}pp:{policyPackId:N}";
}
