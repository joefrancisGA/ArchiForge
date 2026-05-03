/*
  Roll back DbUp 139 — dbo.FindingRecords evaluation confidence columns (dual-write with FindingsJson).
*/

IF OBJECT_ID(N'dbo.FindingRecords', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.FindingRecords', N'EvaluationConfidenceLevel') IS NOT NULL
        ALTER TABLE dbo.FindingRecords DROP COLUMN EvaluationConfidenceLevel;

    IF COL_LENGTH(N'dbo.FindingRecords', N'EvaluationConfidenceScore') IS NOT NULL
        ALTER TABLE dbo.FindingRecords DROP COLUMN EvaluationConfidenceScore;
END;
GO
