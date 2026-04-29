/*
  Scoped audit search by EventType + time descending (pairs with IX_AuditEvents_OccurredUtc_EventId).
*/

IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE name = N'IX_AuditEvents_Scope_EventType_OccurredUtc'
         AND object_id = OBJECT_ID(N'dbo.AuditEvents'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AuditEvents_Scope_EventType_OccurredUtc
        ON dbo.AuditEvents (TenantId, WorkspaceId, ProjectId, EventType, OccurredUtc DESC)
        INCLUDE (EventId, ActorUserId, RunId);
END;
GO
