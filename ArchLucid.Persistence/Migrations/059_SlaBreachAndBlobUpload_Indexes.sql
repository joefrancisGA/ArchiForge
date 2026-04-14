-- 059: Indexes for SLA breach monitoring and blob upload failure diagnostics

-- Filtered index for the SLA monitor background query:
-- Status IN ('Draft','Submitted') AND SlaDeadlineUtc IS NOT NULL AND SlaBreachNotifiedUtc IS NULL
-- ORDER BY SlaDeadlineUtc ASC
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_GovernanceApprovalRequests_PendingSlaBreached'
      AND object_id = OBJECT_ID(N'dbo.GovernanceApprovalRequests'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_GovernanceApprovalRequests_PendingSlaBreached
        ON dbo.GovernanceApprovalRequests (SlaDeadlineUtc ASC)
        INCLUDE (ApprovalRequestId, RunId, RequestedBy, Status)
        WHERE SlaDeadlineUtc IS NOT NULL AND SlaBreachNotifiedUtc IS NULL;
END

-- Approval request status + time ordering for pending and reviewed list queries
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_GovernanceApprovalRequests_Status_RequestedUtc'
      AND object_id = OBJECT_ID(N'dbo.GovernanceApprovalRequests'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_GovernanceApprovalRequests_Status_RequestedUtc
        ON dbo.GovernanceApprovalRequests (Status, RequestedUtc DESC)
        INCLUDE (RunId, ManifestVersion, SourceEnvironment, TargetEnvironment);
END

-- Filtered index for finding traces with failed blob uploads (operator diagnostics)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_AgentExecutionTraces_BlobUploadFailed'
      AND object_id = OBJECT_ID(N'dbo.AgentExecutionTraces'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AgentExecutionTraces_BlobUploadFailed
        ON dbo.AgentExecutionTraces (RunId, CreatedUtc DESC)
        WHERE BlobUploadFailed = 1;
END
