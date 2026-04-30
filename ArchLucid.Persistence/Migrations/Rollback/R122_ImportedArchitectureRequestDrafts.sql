/*
  Rollback 122: remove draft imported architecture request staging table (TOML/JSON upload).
  Forward: 122_ImportedArchitectureRequestDrafts.sql
*/

IF OBJECT_ID(N'dbo.ImportedArchitectureRequests', N'U') IS NOT NULL
    DROP TABLE dbo.ImportedArchitectureRequests;
GO
