-- Evaluation-derived confidence columns for relational findings (dual-write with FindingsJson).

IF OBJECT_ID(N'dbo.FindingRecords', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.FindingRecords', N'EvaluationConfidenceScore') IS NULL
        ALTER TABLE dbo.FindingRecords ADD EvaluationConfidenceScore INT NULL;

    IF COL_LENGTH(N'dbo.FindingRecords', N'EvaluationConfidenceLevel') IS NULL
        ALTER TABLE dbo.FindingRecords ADD EvaluationConfidenceLevel NVARCHAR(20) NULL;
END;
GO
