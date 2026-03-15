namespace ArchiForge.Api.Tests;

public sealed class DeterminismCheckResponse
{
    public DeterminismCheckResultDto Result { get; set; } = new();
}

public sealed class DeterminismCheckResultDto
{
    public string SourceRunId { get; set; } = string.Empty;

    public int Iterations { get; set; }

    public string ExecutionMode { get; set; } = string.Empty;

    public bool IsDeterministic { get; set; }

    public string BaselineReplayRunId { get; set; } = string.Empty;

    public List<DeterminismIterationResultDto> IterationResults { get; set; } = [];

    public List<string> Warnings { get; set; } = [];
}

public sealed class DeterminismIterationResultDto
{
    public int IterationNumber { get; set; }

    public string ReplayRunId { get; set; } = string.Empty;

    public bool MatchesBaselineAgentResults { get; set; }

    public bool MatchesBaselineManifest { get; set; }

    public List<string> AgentDriftWarnings { get; set; } = [];

    public List<string> ManifestDriftWarnings { get; set; } = [];
}
