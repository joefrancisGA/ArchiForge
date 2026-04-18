IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.PolicyPackVersions')
      AND name = N'UQ_PolicyPackVersions_PolicyPackId_Version')
    DROP INDEX UQ_PolicyPackVersions_PolicyPackId_Version ON dbo.PolicyPackVersions;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.PolicyPackVersions')
      AND name = N'IX_PolicyPackVersions_PolicyPackId_Version')
    CREATE NONCLUSTERED INDEX IX_PolicyPackVersions_PolicyPackId_Version
        ON dbo.PolicyPackVersions (PolicyPackId, [Version]);
GO
