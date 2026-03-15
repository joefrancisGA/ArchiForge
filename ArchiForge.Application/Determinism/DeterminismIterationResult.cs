namespace ArchiForge.Application.Determinism;

public sealed class DeterminismIterationResult
{
    public int IterationNumber { get; set; }

    public string ReplayRunId { get; set; } = string.Empty;

    public bool MatchesBaselineAgentResults { get; set; }

    public bool MatchesBaselineManifest { get; set; }

    public List<string> AgentDriftWarnings { get; set; } = [];

    public List<string> ManifestDriftWarnings { get; set; } = [];
}
