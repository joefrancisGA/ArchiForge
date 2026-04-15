namespace ArchLucid.Host.Core.DataConsistency;

/// <summary>
/// SQL for operator-initiated remediation of <see cref="DataConsistencyOrphanProbeSql"/> orphan
/// <c>dbo.ComparisonRecords</c> rows (missing <c>dbo.Runs</c> for left or right run id).
/// </summary>
public static class DataConsistencyOrphanRemediationSql
{
    /// <summary>
    /// Lists up to <c>@MaxRows</c> orphan comparison record ids (oldest first).
    /// </summary>
    public const string SelectOrphanComparisonRecordIds = """
        WITH cte AS (
            SELECT TOP (@MaxRows) c.ComparisonRecordId
            FROM dbo.ComparisonRecords c
            WHERE (
                c.LeftRunId IS NOT NULL
                AND TRY_CONVERT(UNIQUEIDENTIFIER, c.LeftRunId) IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1
                    FROM dbo.Runs r
                    WHERE r.RunId = TRY_CONVERT(UNIQUEIDENTIFIER, c.LeftRunId)))
               OR (
                c.RightRunId IS NOT NULL
                AND TRY_CONVERT(UNIQUEIDENTIFIER, c.RightRunId) IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1
                    FROM dbo.Runs r
                    WHERE r.RunId = TRY_CONVERT(UNIQUEIDENTIFIER, c.RightRunId)))
            ORDER BY c.CreatedUtc ASC
        )
        SELECT ComparisonRecordId FROM cte;
        """;

    /// <summary>
    /// Deletes the same set as <see cref="SelectOrphanComparisonRecordIds"/> and returns deleted ids via <c>OUTPUT</c>.
    /// </summary>
    public const string DeleteOrphanComparisonRecordsWithOutput = """
        WITH cte AS (
            SELECT TOP (@MaxRows) c.ComparisonRecordId
            FROM dbo.ComparisonRecords c
            WHERE (
                c.LeftRunId IS NOT NULL
                AND TRY_CONVERT(UNIQUEIDENTIFIER, c.LeftRunId) IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1
                    FROM dbo.Runs r
                    WHERE r.RunId = TRY_CONVERT(UNIQUEIDENTIFIER, c.LeftRunId)))
               OR (
                c.RightRunId IS NOT NULL
                AND TRY_CONVERT(UNIQUEIDENTIFIER, c.RightRunId) IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1
                    FROM dbo.Runs r
                    WHERE r.RunId = TRY_CONVERT(UNIQUEIDENTIFIER, c.RightRunId)))
            ORDER BY c.CreatedUtc ASC
        )
        DELETE c
        OUTPUT deleted.ComparisonRecordId
        FROM dbo.ComparisonRecords AS c
        INNER JOIN cte ON c.ComparisonRecordId = cte.ComparisonRecordId;
        """;
}
