-- Relational child tables for dbo.ContextSnapshots (dual-write with legacy JSON). Mirrors ArchiForge.Data/SQL/ArchiForge.sql.
IF OBJECT_ID(N'dbo.ContextSnapshotCanonicalObjects', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContextSnapshotCanonicalObjects
    (
        CanonicalObjectRowId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_ContextSnapshotCanonicalObjects PRIMARY KEY,
        SnapshotId UNIQUEIDENTIFIER NOT NULL,
        SortOrder INT NOT NULL,
        ObjectId NVARCHAR(450) NOT NULL,
        ObjectType NVARCHAR(200) NOT NULL,
        Name NVARCHAR(500) NOT NULL,
        SourceType NVARCHAR(200) NOT NULL,
        SourceId NVARCHAR(450) NOT NULL,
        CONSTRAINT FK_ContextSnapshotCanonicalObjects_ContextSnapshots FOREIGN KEY (SnapshotId)
            REFERENCES dbo.ContextSnapshots (SnapshotId) ON DELETE CASCADE,
        CONSTRAINT UQ_ContextSnapshotCanonicalObjects_Snapshot_Sort UNIQUE (SnapshotId, SortOrder)
    );

    CREATE NONCLUSTERED INDEX IX_ContextSnapshotCanonicalObjects_SnapshotId
        ON dbo.ContextSnapshotCanonicalObjects (SnapshotId);
END;

IF OBJECT_ID(N'dbo.ContextSnapshotCanonicalObjectProperties', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContextSnapshotCanonicalObjectProperties
    (
        CanonicalObjectRowId UNIQUEIDENTIFIER NOT NULL,
        PropertySortOrder INT NOT NULL,
        PropertyKey NVARCHAR(200) NOT NULL,
        PropertyValue NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_ContextSnapshotCanonicalObjectProperties PRIMARY KEY (CanonicalObjectRowId, PropertySortOrder),
        CONSTRAINT FK_ContextSnapshotCanonicalObjectProperties_Objects FOREIGN KEY (CanonicalObjectRowId)
            REFERENCES dbo.ContextSnapshotCanonicalObjects (CanonicalObjectRowId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ContextSnapshotCanonicalObjectProperties_Object
        ON dbo.ContextSnapshotCanonicalObjectProperties (CanonicalObjectRowId);
END;

IF OBJECT_ID(N'dbo.ContextSnapshotWarnings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContextSnapshotWarnings
    (
        SnapshotId UNIQUEIDENTIFIER NOT NULL,
        SortOrder INT NOT NULL,
        WarningText NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_ContextSnapshotWarnings PRIMARY KEY (SnapshotId, SortOrder),
        CONSTRAINT FK_ContextSnapshotWarnings_ContextSnapshots FOREIGN KEY (SnapshotId)
            REFERENCES dbo.ContextSnapshots (SnapshotId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ContextSnapshotWarnings_SnapshotId
        ON dbo.ContextSnapshotWarnings (SnapshotId);
END;

IF OBJECT_ID(N'dbo.ContextSnapshotErrors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContextSnapshotErrors
    (
        SnapshotId UNIQUEIDENTIFIER NOT NULL,
        SortOrder INT NOT NULL,
        ErrorText NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_ContextSnapshotErrors PRIMARY KEY (SnapshotId, SortOrder),
        CONSTRAINT FK_ContextSnapshotErrors_ContextSnapshots FOREIGN KEY (SnapshotId)
            REFERENCES dbo.ContextSnapshots (SnapshotId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ContextSnapshotErrors_SnapshotId
        ON dbo.ContextSnapshotErrors (SnapshotId);
END;

IF OBJECT_ID(N'dbo.ContextSnapshotSourceHashes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContextSnapshotSourceHashes
    (
        SnapshotId UNIQUEIDENTIFIER NOT NULL,
        SortOrder INT NOT NULL,
        SourceKey NVARCHAR(450) NOT NULL,
        HashValue NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_ContextSnapshotSourceHashes PRIMARY KEY (SnapshotId, SortOrder),
        CONSTRAINT FK_ContextSnapshotSourceHashes_ContextSnapshots FOREIGN KEY (SnapshotId)
            REFERENCES dbo.ContextSnapshots (SnapshotId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ContextSnapshotSourceHashes_SnapshotId
        ON dbo.ContextSnapshotSourceHashes (SnapshotId);
END;
