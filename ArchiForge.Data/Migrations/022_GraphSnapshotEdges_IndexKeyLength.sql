-- Replace IX_GraphSnapshotEdges_FromTo: (GraphSnapshotId + NVARCHAR(500) + NVARCHAR(500)) exceeds SQL Server's
-- 1700-byte nonclustered index key limit (2016 bytes). Use key (GraphSnapshotId, FromNodeId) with INCLUDE (ToNodeId, ...).
IF EXISTS (
    SELECT 1
    FROM sys.indexes i
    WHERE i.name = N'IX_GraphSnapshotEdges_FromTo'
      AND i.object_id = OBJECT_ID(N'dbo.GraphSnapshotEdges'))
BEGIN
    DROP INDEX IX_GraphSnapshotEdges_FromTo ON dbo.GraphSnapshotEdges;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
    WHERE i.name = N'IX_GraphSnapshotEdges_SnapshotFrom'
      AND i.object_id = OBJECT_ID(N'dbo.GraphSnapshotEdges'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_GraphSnapshotEdges_SnapshotFrom
        ON dbo.GraphSnapshotEdges (GraphSnapshotId, FromNodeId)
        INCLUDE (ToNodeId, EdgeType, Weight);
END;
GO
