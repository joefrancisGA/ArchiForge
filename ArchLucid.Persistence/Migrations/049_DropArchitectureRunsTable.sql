/*
  Migration 049: Drop legacy dbo.ArchitectureRuns.

  Authority run state lives in dbo.Runs (UNIQUEIDENTIFIER RunId). Migration 047 removed inbound FKs
  from coordinator / learning tables to ArchitectureRuns; this migration retires the table.

  Idempotent: DROP only when the table exists.
*/

IF OBJECT_ID(N'dbo.ArchitectureRuns', N'U') IS NOT NULL
    DROP TABLE dbo.ArchitectureRuns;
GO
