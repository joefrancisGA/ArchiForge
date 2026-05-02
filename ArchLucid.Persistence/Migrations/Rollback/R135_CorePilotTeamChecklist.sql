/*
  Reverse DbUp 135 — dbo.CorePilotTeamChecklist (team-visible Core Pilot milestones at triple scope).
*/

IF DATABASE_PRINCIPAL_ID(N'ArchLucidApp') IS NOT NULL
   AND OBJECT_ID(N'dbo.CorePilotTeamChecklist', N'U') IS NOT NULL
BEGIN
    REVOKE SELECT, INSERT, UPDATE ON dbo.CorePilotTeamChecklist TO [ArchLucidApp];
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.CorePilotTeamChecklist', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.CorePilotTeamChecklist,
        DROP BLOCK PREDICATE ON dbo.CorePilotTeamChecklist FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.CorePilotTeamChecklist FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.CorePilotTeamChecklist FOR BEFORE DELETE;
END;
GO

IF OBJECT_ID(N'dbo.CorePilotTeamChecklist', N'U') IS NOT NULL
    DROP TABLE dbo.CorePilotTeamChecklist;
GO
