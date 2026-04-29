IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_BackgroundJobs_State_StartedUtc_Running'
      AND object_id = OBJECT_ID(N'dbo.BackgroundJobs'))
BEGIN
    DROP INDEX IX_BackgroundJobs_State_StartedUtc_Running ON dbo.BackgroundJobs;
END;
GO
