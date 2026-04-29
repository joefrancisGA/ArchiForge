/*
  DbUp 130: Governance approval segregation-of-duties — canonical JWT actor keys alongside display names.
  Greenfield parity: ArchLucid.Persistence/Scripts/ArchLucid.sql.
*/

SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.GovernanceApprovalRequests', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.GovernanceApprovalRequests', N'RequestedByActorKey') IS NULL
    ALTER TABLE dbo.GovernanceApprovalRequests ADD RequestedByActorKey NVARCHAR(256) NULL;

IF OBJECT_ID(N'dbo.GovernanceApprovalRequests', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.GovernanceApprovalRequests', N'ReviewedByActorKey') IS NULL
    ALTER TABLE dbo.GovernanceApprovalRequests ADD ReviewedByActorKey NVARCHAR(256) NULL;
GO
