/*
  Filtered nonclustered indexes on dbo.AuditEvents for correlation-id lookups and per-run timelines.
  See docs/AUDIT_COVERAGE_MATRIX.md (indexes subsection).
*/

IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_AuditEvents_CorrelationId'
          AND object_id = OBJECT_ID(N'dbo.AuditEvents'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AuditEvents_CorrelationId
        ON dbo.AuditEvents (CorrelationId)
        WHERE CorrelationId IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_AuditEvents_RunId_OccurredUtc'
          AND object_id = OBJECT_ID(N'dbo.AuditEvents'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AuditEvents_RunId_OccurredUtc
        ON dbo.AuditEvents (RunId, OccurredUtc DESC)
        WHERE RunId IS NOT NULL;
END;
GO
