-- 058: SLA deadline and breach notification tracking on governance approval requests
IF COL_LENGTH('dbo.GovernanceApprovalRequests', 'SlaDeadlineUtc') IS NULL
BEGIN
    ALTER TABLE dbo.GovernanceApprovalRequests
        ADD SlaDeadlineUtc DATETIME2 NULL;
END

IF COL_LENGTH('dbo.GovernanceApprovalRequests', 'SlaBreachNotifiedUtc') IS NULL
BEGIN
    ALTER TABLE dbo.GovernanceApprovalRequests
        ADD SlaBreachNotifiedUtc DATETIME2 NULL;
END
