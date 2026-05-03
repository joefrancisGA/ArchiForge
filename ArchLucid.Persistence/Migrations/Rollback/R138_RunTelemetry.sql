/*
  Roll back DbUp 138 — dbo.RunTelemetry (run-level timing / savings telemetry).
*/

IF OBJECT_ID(N'dbo.RunTelemetry', N'U') IS NOT NULL
    DROP TABLE dbo.RunTelemetry;
GO
