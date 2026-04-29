IF COL_LENGTH(N'dbo.GovernanceApprovalRequests', N'ReviewedByActorKey') IS NOT NULL
    ALTER TABLE dbo.GovernanceApprovalRequests DROP COLUMN ReviewedByActorKey;

IF COL_LENGTH(N'dbo.GovernanceApprovalRequests', N'RequestedByActorKey') IS NOT NULL
    ALTER TABLE dbo.GovernanceApprovalRequests DROP COLUMN RequestedByActorKey;
GO
