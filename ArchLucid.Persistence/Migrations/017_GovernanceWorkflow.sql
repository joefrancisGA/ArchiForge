CREATE TABLE GovernanceApprovalRequests (
    ApprovalRequestId NVARCHAR(64) NOT NULL PRIMARY KEY,
    RunId NVARCHAR(64) NOT NULL,
    ManifestVersion NVARCHAR(128) NOT NULL,
    SourceEnvironment NVARCHAR(32) NOT NULL,
    TargetEnvironment NVARCHAR(32) NOT NULL,
    Status NVARCHAR(32) NOT NULL,
    RequestedBy NVARCHAR(200) NOT NULL,
    ReviewedBy NVARCHAR(200) NULL,
    RequestComment NVARCHAR(MAX) NULL,
    ReviewComment NVARCHAR(MAX) NULL,
    RequestedUtc DATETIME2 NOT NULL,
    ReviewedUtc DATETIME2 NULL
);

CREATE TABLE GovernancePromotionRecords (
    PromotionRecordId NVARCHAR(64) NOT NULL PRIMARY KEY,
    RunId NVARCHAR(64) NOT NULL,
    ManifestVersion NVARCHAR(128) NOT NULL,
    SourceEnvironment NVARCHAR(32) NOT NULL,
    TargetEnvironment NVARCHAR(32) NOT NULL,
    PromotedBy NVARCHAR(200) NOT NULL,
    PromotedUtc DATETIME2 NOT NULL,
    ApprovalRequestId NVARCHAR(64) NULL,
    Notes NVARCHAR(MAX) NULL
);

CREATE TABLE GovernanceEnvironmentActivations (
    ActivationId NVARCHAR(64) NOT NULL PRIMARY KEY,
    RunId NVARCHAR(64) NOT NULL,
    ManifestVersion NVARCHAR(128) NOT NULL,
    Environment NVARCHAR(32) NOT NULL,
    IsActive BIT NOT NULL,
    ActivatedUtc DATETIME2 NOT NULL
);

CREATE INDEX IX_GovernanceApprovalRequests_RunId
ON GovernanceApprovalRequests(RunId);

CREATE INDEX IX_GovernancePromotionRecords_RunId
ON GovernancePromotionRecords(RunId);

CREATE INDEX IX_GovernanceEnvironmentActivations_Environment_IsActive
ON GovernanceEnvironmentActivations(Environment, IsActive);
