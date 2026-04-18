SET NOCOUNT ON;
GO

/* Remove duplicate (PolicyPackId, Version) rows before enforcing uniqueness (keep newest CreatedUtc). */
;WITH Ranked AS (
    SELECT
        PolicyPackVersionId,
        ROW_NUMBER() OVER (
            PARTITION BY PolicyPackId, [Version]
            ORDER BY CreatedUtc DESC, PolicyPackVersionId DESC) AS rn
    FROM dbo.PolicyPackVersions
)
DELETE v
FROM dbo.PolicyPackVersions v
INNER JOIN Ranked r ON r.PolicyPackVersionId = v.PolicyPackVersionId
WHERE r.rn > 1;
GO

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.PolicyPackVersions')
      AND name = N'IX_PolicyPackVersions_PolicyPackId_Version')
    DROP INDEX IX_PolicyPackVersions_PolicyPackId_Version ON dbo.PolicyPackVersions;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.PolicyPackVersions')
      AND name = N'UQ_PolicyPackVersions_PolicyPackId_Version')
    CREATE UNIQUE NONCLUSTERED INDEX UQ_PolicyPackVersions_PolicyPackId_Version
        ON dbo.PolicyPackVersions (PolicyPackId, [Version]);
GO
