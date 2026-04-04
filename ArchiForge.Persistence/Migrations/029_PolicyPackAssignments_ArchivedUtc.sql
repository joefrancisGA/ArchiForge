-- Soft-delete / archival for governance assignments: archived rows stay for audit but are excluded from resolution lists.

IF OBJECT_ID(N'dbo.PolicyPackAssignments', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.PolicyPackAssignments', 'ArchivedUtc') IS NULL
    ALTER TABLE dbo.PolicyPackAssignments ADD ArchivedUtc DATETIME2 NULL;

GO
