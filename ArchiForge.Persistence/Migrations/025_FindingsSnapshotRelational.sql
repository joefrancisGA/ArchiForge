-- Relational findings + header SchemaVersion (dual-write with FindingsJson). Mirrors ArchiForge.Persistence/Scripts/ArchiForge.sql.
-- FindingsSnapshots is created here if missing (reference script ArchiForge.sql is not applied by DbUp).
IF OBJECT_ID(N'dbo.FindingsSnapshots', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingsSnapshots
    (
        FindingsSnapshotId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        ContextSnapshotId UNIQUEIDENTIFIER NOT NULL,
        GraphSnapshotId UNIQUEIDENTIFIER NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        SchemaVersion INT NOT NULL DEFAULT (1),
        FindingsJson NVARCHAR(MAX) NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_FindingsSnapshots_RunId ON dbo.FindingsSnapshots (RunId);
    CREATE NONCLUSTERED INDEX IX_FindingsSnapshots_ContextSnapshotId ON dbo.FindingsSnapshots (ContextSnapshotId);
    CREATE NONCLUSTERED INDEX IX_FindingsSnapshots_GraphSnapshotId ON dbo.FindingsSnapshots (GraphSnapshotId);
END;

IF OBJECT_ID(N'dbo.FindingsSnapshots', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.FindingsSnapshots', N'SchemaVersion') IS NULL
BEGIN
    ALTER TABLE dbo.FindingsSnapshots
        ADD SchemaVersion INT NOT NULL CONSTRAINT DF_FindingsSnapshots_SchemaVersion_Brownfield DEFAULT (1);
END;

IF OBJECT_ID(N'dbo.FindingsSnapshots', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FindingsSnapshots_Runs_RunId')
BEGIN
    ALTER TABLE dbo.FindingsSnapshots ADD CONSTRAINT FK_FindingsSnapshots_Runs_RunId
        FOREIGN KEY (RunId) REFERENCES dbo.Runs (RunId);
END;

IF OBJECT_ID(N'dbo.FindingsSnapshots', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.ContextSnapshots', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FindingsSnapshots_ContextSnapshots_ContextSnapshotId')
BEGIN
    ALTER TABLE dbo.FindingsSnapshots ADD CONSTRAINT FK_FindingsSnapshots_ContextSnapshots_ContextSnapshotId
        FOREIGN KEY (ContextSnapshotId) REFERENCES dbo.ContextSnapshots (SnapshotId);
END;

IF OBJECT_ID(N'dbo.FindingsSnapshots', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.GraphSnapshots', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FindingsSnapshots_GraphSnapshots_GraphSnapshotId')
BEGIN
    ALTER TABLE dbo.FindingsSnapshots ADD CONSTRAINT FK_FindingsSnapshots_GraphSnapshots_GraphSnapshotId
        FOREIGN KEY (GraphSnapshotId) REFERENCES dbo.GraphSnapshots (GraphSnapshotId);
END;

IF OBJECT_ID(N'dbo.FindingRecords', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingRecords
    (
        FindingRecordId      UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_FindingRecords PRIMARY KEY,
        FindingsSnapshotId   UNIQUEIDENTIFIER NOT NULL,
        SortOrder            INT NOT NULL,
        FindingId            NVARCHAR(200) NOT NULL,
        FindingSchemaVersion INT NOT NULL,
        FindingType          NVARCHAR(200) NOT NULL,
        Category             NVARCHAR(200) NOT NULL,
        EngineType           NVARCHAR(200) NOT NULL,
        Severity             NVARCHAR(50) NOT NULL,
        Title                NVARCHAR(1000) NOT NULL,
        Rationale            NVARCHAR(MAX) NOT NULL,
        PayloadType          NVARCHAR(256) NULL,
        PayloadJson          NVARCHAR(MAX) NULL,
        CONSTRAINT FK_FindingRecords_FindingsSnapshots FOREIGN KEY (FindingsSnapshotId)
            REFERENCES dbo.FindingsSnapshots (FindingsSnapshotId) ON DELETE CASCADE,
        CONSTRAINT UQ_FindingRecords_Snapshot_Sort UNIQUE (FindingsSnapshotId, SortOrder)
    );

    CREATE NONCLUSTERED INDEX IX_FindingRecords_FindingsSnapshotId
        ON dbo.FindingRecords (FindingsSnapshotId);
END;

IF OBJECT_ID(N'dbo.FindingRelatedNodes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingRelatedNodes
    (
        FindingRecordId UNIQUEIDENTIFIER NOT NULL,
        SortOrder       INT NOT NULL,
        NodeId          NVARCHAR(500) NOT NULL,
        CONSTRAINT PK_FindingRelatedNodes PRIMARY KEY (FindingRecordId, SortOrder),
        CONSTRAINT FK_FindingRelatedNodes_FindingRecords FOREIGN KEY (FindingRecordId)
            REFERENCES dbo.FindingRecords (FindingRecordId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_FindingRelatedNodes_Record
        ON dbo.FindingRelatedNodes (FindingRecordId);
END;

IF OBJECT_ID(N'dbo.FindingRecommendedActions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingRecommendedActions
    (
        FindingRecordId UNIQUEIDENTIFIER NOT NULL,
        SortOrder       INT NOT NULL,
        ActionText      NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_FindingRecommendedActions PRIMARY KEY (FindingRecordId, SortOrder),
        CONSTRAINT FK_FindingRecommendedActions_FindingRecords FOREIGN KEY (FindingRecordId)
            REFERENCES dbo.FindingRecords (FindingRecordId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_FindingRecommendedActions_Record
        ON dbo.FindingRecommendedActions (FindingRecordId);
END;

IF OBJECT_ID(N'dbo.FindingProperties', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingProperties
    (
        FindingRecordId   UNIQUEIDENTIFIER NOT NULL,
        PropertySortOrder INT NOT NULL,
        PropertyKey       NVARCHAR(200) NOT NULL,
        PropertyValue     NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_FindingProperties PRIMARY KEY (FindingRecordId, PropertySortOrder),
        CONSTRAINT FK_FindingProperties_FindingRecords FOREIGN KEY (FindingRecordId)
            REFERENCES dbo.FindingRecords (FindingRecordId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_FindingProperties_Record
        ON dbo.FindingProperties (FindingRecordId);
END;

IF OBJECT_ID(N'dbo.FindingTraceGraphNodesExamined', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingTraceGraphNodesExamined
    (
        FindingRecordId UNIQUEIDENTIFIER NOT NULL,
        SortOrder       INT NOT NULL,
        NodeId          NVARCHAR(500) NOT NULL,
        CONSTRAINT PK_FindingTraceGraphNodesExamined PRIMARY KEY (FindingRecordId, SortOrder),
        CONSTRAINT FK_FindingTraceGraphNodesExamined_FindingRecords FOREIGN KEY (FindingRecordId)
            REFERENCES dbo.FindingRecords (FindingRecordId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_FindingTraceGraphNodesExamined_Record
        ON dbo.FindingTraceGraphNodesExamined (FindingRecordId);
END;

IF OBJECT_ID(N'dbo.FindingTraceRulesApplied', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingTraceRulesApplied
    (
        FindingRecordId UNIQUEIDENTIFIER NOT NULL,
        SortOrder       INT NOT NULL,
        RuleText        NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_FindingTraceRulesApplied PRIMARY KEY (FindingRecordId, SortOrder),
        CONSTRAINT FK_FindingTraceRulesApplied_FindingRecords FOREIGN KEY (FindingRecordId)
            REFERENCES dbo.FindingRecords (FindingRecordId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_FindingTraceRulesApplied_Record
        ON dbo.FindingTraceRulesApplied (FindingRecordId);
END;

IF OBJECT_ID(N'dbo.FindingTraceDecisionsTaken', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingTraceDecisionsTaken
    (
        FindingRecordId UNIQUEIDENTIFIER NOT NULL,
        SortOrder       INT NOT NULL,
        DecisionText    NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_FindingTraceDecisionsTaken PRIMARY KEY (FindingRecordId, SortOrder),
        CONSTRAINT FK_FindingTraceDecisionsTaken_FindingRecords FOREIGN KEY (FindingRecordId)
            REFERENCES dbo.FindingRecords (FindingRecordId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_FindingTraceDecisionsTaken_Record
        ON dbo.FindingTraceDecisionsTaken (FindingRecordId);
END;

IF OBJECT_ID(N'dbo.FindingTraceAlternativePaths', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingTraceAlternativePaths
    (
        FindingRecordId UNIQUEIDENTIFIER NOT NULL,
        SortOrder       INT NOT NULL,
        PathText        NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_FindingTraceAlternativePaths PRIMARY KEY (FindingRecordId, SortOrder),
        CONSTRAINT FK_FindingTraceAlternativePaths_FindingRecords FOREIGN KEY (FindingRecordId)
            REFERENCES dbo.FindingRecords (FindingRecordId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_FindingTraceAlternativePaths_Record
        ON dbo.FindingTraceAlternativePaths (FindingRecordId);
END;

IF OBJECT_ID(N'dbo.FindingTraceNotes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingTraceNotes
    (
        FindingRecordId UNIQUEIDENTIFIER NOT NULL,
        SortOrder       INT NOT NULL,
        NoteText        NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_FindingTraceNotes PRIMARY KEY (FindingRecordId, SortOrder),
        CONSTRAINT FK_FindingTraceNotes_FindingRecords FOREIGN KEY (FindingRecordId)
            REFERENCES dbo.FindingRecords (FindingRecordId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_FindingTraceNotes_Record
        ON dbo.FindingTraceNotes (FindingRecordId);
END;
