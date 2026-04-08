-- Legacy dual-write JSON columns on GraphSnapshots may be unset when a header row is inserted before JSON backfill.
IF OBJECT_ID(N'dbo.GraphSnapshots', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        INNER JOIN sys.tables t ON c.object_id = t.object_id
        WHERE t.schema_id = SCHEMA_ID(N'dbo')
          AND t.name = N'GraphSnapshots'
          AND c.name = N'NodesJson'
          AND c.is_nullable = 0)
        ALTER TABLE dbo.GraphSnapshots ALTER COLUMN NodesJson NVARCHAR(MAX) NULL;

    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        INNER JOIN sys.tables t ON c.object_id = t.object_id
        WHERE t.schema_id = SCHEMA_ID(N'dbo')
          AND t.name = N'GraphSnapshots'
          AND c.name = N'EdgesJson'
          AND c.is_nullable = 0)
        ALTER TABLE dbo.GraphSnapshots ALTER COLUMN EdgesJson NVARCHAR(MAX) NULL;

    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        INNER JOIN sys.tables t ON c.object_id = t.object_id
        WHERE t.schema_id = SCHEMA_ID(N'dbo')
          AND t.name = N'GraphSnapshots'
          AND c.name = N'WarningsJson'
          AND c.is_nullable = 0)
        ALTER TABLE dbo.GraphSnapshots ALTER COLUMN WarningsJson NVARCHAR(MAX) NULL;
END;
