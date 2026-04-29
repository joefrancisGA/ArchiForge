IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_FindingRecords_Snapshot_Category'
      AND object_id = OBJECT_ID(N'dbo.FindingRecords'))
BEGIN
    DROP INDEX IX_FindingRecords_Snapshot_Category ON dbo.FindingRecords;
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_FindingRecords_Snapshot_Severity'
      AND object_id = OBJECT_ID(N'dbo.FindingRecords'))
BEGIN
    DROP INDEX IX_FindingRecords_Snapshot_Severity ON dbo.FindingRecords;
END;
GO
