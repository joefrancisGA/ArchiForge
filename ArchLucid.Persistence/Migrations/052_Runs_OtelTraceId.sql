IF OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Runs', N'OtelTraceId') IS NULL
    ALTER TABLE dbo.Runs ADD OtelTraceId NVARCHAR(64) NULL;
GO
