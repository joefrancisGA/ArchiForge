-- Decision nodes (Decision Engine v2 output) + agent evaluations

CREATE TABLE DecisionNodes
(
    DecisionId NVARCHAR(64) NOT NULL PRIMARY KEY,
    RunId NVARCHAR(64) NOT NULL,
    Topic NVARCHAR(100) NOT NULL,
    SelectedOptionId NVARCHAR(64) NULL,
    Confidence FLOAT NOT NULL,
    Rationale NVARCHAR(MAX) NOT NULL,
    DecisionJson NVARCHAR(MAX) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL
);

CREATE INDEX IX_DecisionNodes_RunId
    ON DecisionNodes (RunId);

CREATE TABLE AgentEvaluations
(
    EvaluationId NVARCHAR(64) NOT NULL PRIMARY KEY,
    RunId NVARCHAR(64) NOT NULL,
    TargetAgentTaskId NVARCHAR(64) NOT NULL,
    EvaluationType NVARCHAR(50) NOT NULL,
    ConfidenceDelta FLOAT NOT NULL,
    Rationale NVARCHAR(MAX) NOT NULL,
    EvaluationJson NVARCHAR(MAX) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL
);

CREATE INDEX IX_AgentEvaluations_RunId
    ON AgentEvaluations (RunId);

