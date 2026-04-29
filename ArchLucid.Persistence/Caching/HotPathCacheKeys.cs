using ArchLucid.Core.Scoping;

namespace ArchLucid.Persistence.Caching;

/// <summary>Stable cache key fragments for hot-path repository decorators (tenant-scoped where applicable).</summary>
public static class HotPathCacheKeys
{
    private const string Prefix = "al:hot:";

    /// <summary>Legacy <c>af:hot:</c> prefix (pre-rename); eviction and read promotion still honor these keys.</summary>
    private const string LegacyPrefix = "af:hot:";

    /// <summary>Golden manifest by authority scope + manifest id.</summary>
    public static string Manifest(ScopeContext scope, Guid manifestId)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return $"{Prefix}hm:{scope.TenantId:N}:{scope.WorkspaceId:N}:{scope.ProjectId:N}:{manifestId:N}";
    }

    /// <summary>Pre-rename manifest key shape (read promotion / eviction only).</summary>
    public static string LegacyManifest(ScopeContext scope, Guid manifestId)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return $"{LegacyPrefix}hm:{scope.TenantId:N}:{scope.WorkspaceId:N}:{scope.ProjectId:N}:{manifestId:N}";
    }

    /// <summary>Authority run row by scope + run id (matches <c>dbo.Runs</c> scope columns).</summary>
    public static string Run(ScopeContext scope, Guid runId)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return $"{Prefix}run:{scope.TenantId:N}:{scope.WorkspaceId:N}:{scope.ProjectId:N}:{runId:N}";
    }

    /// <summary>Pre-rename run key shape (read promotion / eviction only).</summary>
    public static string LegacyRun(ScopeContext scope, Guid runId)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return $"{LegacyPrefix}run:{scope.TenantId:N}:{scope.WorkspaceId:N}:{scope.ProjectId:N}:{runId:N}";
    }

    /// <summary>First keyset page lists (project slug + take) — TTL short (<c>15s</c>).</summary>
    public static string RunListByProjectFirstPage(ScopeContext scope, string projectSlug, int take)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return $"{Prefix}runlist:proj:{scope.TenantId:N}:{scope.WorkspaceId:N}:{scope.ProjectId:N}:{take:D}:{projectSlug}";
    }

    /// <summary>First keyset page lists (recent in scope).</summary>
    public static string RunListRecentInScopeFirstPage(ScopeContext scope, int take)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return $"{Prefix}runlist:scope:{scope.TenantId:N}:{scope.WorkspaceId:N}:{scope.ProjectId:N}:{take:D}";
    }

    /// <summary>Policy pack metadata by surrogate key.</summary>
    public static string PolicyPack(Guid policyPackId)
    {
        return $"{Prefix}pp:{policyPackId:N}";
    }

    /// <summary>Pre-rename policy pack key shape (read promotion / eviction only).</summary>
    public static string LegacyPolicyPack(Guid policyPackId)
    {
        return $"{LegacyPrefix}pp:{policyPackId:N}";
    }
}
