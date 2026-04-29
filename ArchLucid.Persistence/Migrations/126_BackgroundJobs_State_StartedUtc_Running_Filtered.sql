/* Stuck-job watchdog: seek Running rows by StartedUtc without scanning Completed jobs. */

IF OBJECT_ID(N'dbo.BackgroundJobs', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE name = N'IX_BackgroundJobs_State_StartedUtc_Running'
         AND object_id = OBJECT_ID(N'dbo.BackgroundJobs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BackgroundJobs_State_StartedUtc_Running
        ON dbo.BackgroundJobs (StartedUtc DESC)
        WHERE State = N'Running';
END;
GO
