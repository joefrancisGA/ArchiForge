namespace ArchLucid.Host.Core.DataConsistency;

/// <summary>Top-N tenant rollup for orphan probes (log-only; keeps Prometheus cardinality bounded).</summary>
internal static class DataConsistencyOrphanTenantBreakdownSql
{
    /// <summary>Parameters: <c>@TopN</c> (INT).</summary>
    public const string GoldenManifestsByTenant = """
        SELECT TOP (@TopN)
            CAST(g.TenantId AS NVARCHAR(36)) AS TenantKey,
            COUNT_BIG(1) AS OrphanCount
        FROM dbo.GoldenManifests g
        WHERE NOT EXISTS (
            SELECT 1
            FROM dbo.Runs r
            WHERE r.RunId = g.RunId)
        GROUP BY g.TenantId
        ORDER BY OrphanCount DESC, TenantKey ASC;
        """;

    /// <summary>Parameters: <c>@TopN</c> (INT).</summary>
    public const string FindingsSnapshotsByTenant = """
        SELECT TOP (@TopN)
            ISNULL(CAST(f.TenantId AS NVARCHAR(36)), N'(null)') AS TenantKey,
            COUNT_BIG(1) AS OrphanCount
        FROM dbo.FindingsSnapshots f
        WHERE NOT EXISTS (
            SELECT 1
            FROM dbo.Runs r
            WHERE r.RunId = f.RunId)
        GROUP BY f.TenantId
        ORDER BY OrphanCount DESC, TenantKey ASC;
        """;

    /// <summary>Parameters: <c>@TopN</c> (INT).</summary>
    public const string ContextSnapshotsByTenant = """
        SELECT TOP (@TopN)
            ISNULL(CAST(c.TenantId AS NVARCHAR(36)), N'(null)') AS TenantKey,
            COUNT_BIG(1) AS OrphanCount
        FROM dbo.ContextSnapshots c
        WHERE NOT EXISTS (
            SELECT 1
            FROM dbo.Runs r
            WHERE r.RunId = c.RunId)
        GROUP BY c.TenantId
        ORDER BY OrphanCount DESC, TenantKey ASC;
        """;
}
