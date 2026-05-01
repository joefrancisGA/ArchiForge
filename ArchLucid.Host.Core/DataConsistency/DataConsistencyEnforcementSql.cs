namespace ArchLucid.Host.Core.DataConsistency;

/// <summary>Inserts orphan <c>dbo.GoldenManifests</c> rows into quarantine (idempotent on <c>SourceRowKey</c>).</summary>
internal static class DataConsistencyEnforcementSql
{
    /// <summary>
    ///     Round-robin selection across <c>TenantId</c> using <c>ROW_NUMBER()</c> so one noisy tenant cannot consume the
    ///     entire batch cap.
    /// </summary>
    public const string InsertOrphanGoldenManifestsMissingRun = """
        INSERT INTO dbo.DataConsistencyQuarantine (QuarantineId, TenantId, SourceTable, SourceColumn, SourceRowKey, DetectedUtc, ReasonJson)
        SELECT TOP (@MaxRows)
            NEWID() AS QuarantineId,
            rr.TenantId,
            N'GoldenManifests' AS SourceTable,
            N'RunId' AS SourceColumn,
            rr.SourceRowKey,
            SYSUTCDATETIME() AS DetectedUtc,
            N'{"kind":"orphan_missing_run"}' AS ReasonJson
        FROM (
            SELECT
                g.ManifestId,
                g.TenantId,
                CAST(g.ManifestId AS NVARCHAR(36)) AS SourceRowKey,
                ROW_NUMBER() OVER (PARTITION BY g.TenantId ORDER BY g.CreatedUtc ASC, g.ManifestId ASC)
                    AS tenantRoundRobin
            FROM dbo.GoldenManifests g
            WHERE NOT EXISTS (
                SELECT 1 FROM dbo.Runs r WHERE r.RunId = g.RunId)
              AND NOT EXISTS (
                SELECT 1
                FROM dbo.DataConsistencyQuarantine q
                WHERE q.SourceTable = N'GoldenManifests'
                  AND q.SourceColumn = N'RunId'
                  AND q.SourceRowKey = CAST(g.ManifestId AS NVARCHAR(36)))
        ) rr
        ORDER BY rr.tenantRoundRobin ASC, rr.TenantId ASC;
        """;
}
