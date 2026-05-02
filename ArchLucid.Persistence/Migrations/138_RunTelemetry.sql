CREATE TABLE [dbo].[RunTelemetry] (
    [RunId] UNIQUEIDENTIFIER NOT NULL,
    [RequestDurationMs] BIGINT NOT NULL,
    [AgentExecutionDurationMs] BIGINT NOT NULL,
    [ManualReviewDurationMs] BIGINT NOT NULL,
    [EstimatedHoursSaved] DECIMAL(18,2) NOT NULL,
    CONSTRAINT [PK_RunTelemetry] PRIMARY KEY CLUSTERED ([RunId]),
    CONSTRAINT [FK_RunTelemetry_Runs] FOREIGN KEY ([RunId]) REFERENCES [dbo].[Runs] ([RunId]) ON DELETE CASCADE
);
GO
