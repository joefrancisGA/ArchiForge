CREATE TABLE ComparisonRecords
(
    ComparisonRecordId NVARCHAR(64) NOT NULL PRIMARY KEY,
    ComparisonType NVARCHAR(100) NOT NULL,
    LeftRunId NVARCHAR(64) NULL,
    RightRunId NVARCHAR(64) NULL,
    LeftManifestVersion NVARCHAR(100) NULL,
    RightManifestVersion NVARCHAR(100) NULL,
    LeftExportRecordId NVARCHAR(64) NULL,
    RightExportRecordId NVARCHAR(64) NULL,
    Format NVARCHAR(50) NOT NULL,
    SummaryMarkdown NVARCHAR(MAX) NULL,
    PayloadJson NVARCHAR(MAX) NOT NULL,
    Notes NVARCHAR(MAX) NULL,
    CreatedUtc DATETIME2 NOT NULL
);

CREATE INDEX IX_ComparisonRecords_LeftRunId ON ComparisonRecords (LeftRunId);
CREATE INDEX IX_ComparisonRecords_RightRunId ON ComparisonRecords (RightRunId);
CREATE INDEX IX_ComparisonRecords_LeftExportRecordId ON ComparisonRecords (LeftExportRecordId);
CREATE INDEX IX_ComparisonRecords_RightExportRecordId ON ComparisonRecords (RightExportRecordId);

