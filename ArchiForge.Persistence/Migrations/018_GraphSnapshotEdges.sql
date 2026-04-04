-- Denormalized edges for indexed queries without deserializing EdgesJson.
IF OBJECT_ID(N'dbo.GraphSnapshotEdges', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GraphSnapshotEdges
    (
        GraphSnapshotId UNIQUEIDENTIFIER NOT NULL,
        EdgeId            NVARCHAR(200) NOT NULL,
        FromNodeId        NVARCHAR(500) NOT NULL,
        ToNodeId          NVARCHAR(500) NOT NULL,
        EdgeType          NVARCHAR(100) NOT NULL,
        Weight            FLOAT NOT NULL
            CONSTRAINT DF_GraphSnapshotEdges_Weight DEFAULT (1),
        CONSTRAINT PK_GraphSnapshotEdges PRIMARY KEY (GraphSnapshotId, EdgeId),
        CONSTRAINT FK_GraphSnapshotEdges_GraphSnapshots FOREIGN KEY (GraphSnapshotId)
            REFERENCES dbo.GraphSnapshots (GraphSnapshotId)
    );

    -- Index key (GraphSnapshotId, FromNodeId, ToNodeId) exceeds 1700-byte SQL Server limit; use INCLUDE for ToNodeId.
    CREATE NONCLUSTERED INDEX IX_GraphSnapshotEdges_SnapshotFrom
        ON dbo.GraphSnapshotEdges (GraphSnapshotId, FromNodeId)
        INCLUDE (ToNodeId, EdgeType, Weight);
END;
