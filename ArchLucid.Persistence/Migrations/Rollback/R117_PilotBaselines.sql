SET NOCOUNT ON;
GO

/* R117: Roll back 117_PilotBaselines.sql — drop in-product pilot baseline rows and table. */

IF OBJECT_ID(N'dbo.PilotBaselines', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.PilotBaselines;
END;
GO
