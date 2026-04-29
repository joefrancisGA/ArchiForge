/* Indexed filters for dbo.FindingRecords list queries within a snapshot. */

IF OBJECT_ID(N'dbo.FindingRecords', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE name = N'IX_FindingRecords_Snapshot_Severity'
         AND object_id = OBJECT_ID(N'dbo.FindingRecords'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_FindingRecords_Snapshot_Severity
        ON dbo.FindingRecords (FindingsSnapshotId, Severity, SortOrder)
        INCLUDE (FindingRecordId, FindingId, Category, EngineType, Title);
END;
GO

IF OBJECT_ID(N'dbo.FindingRecords', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE name = N'IX_FindingRecords_Snapshot_Category'
         AND object_id = OBJECT_ID(N'dbo.FindingRecords'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_FindingRecords_Snapshot_Category
        ON dbo.FindingRecords (FindingsSnapshotId, Category, SortOrder)
        INCLUDE (FindingRecordId, Severity, FindingType, Title);
END;
GO
