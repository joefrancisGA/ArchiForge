namespace ArchiForge.Api.Models.Evolution;

/// <summary>Portable simulation report (JSON export body).</summary>
public sealed class EvolutionSimulationReportDocument
{
    public const string ExportSchemaVersion = "60R-simulation-export-v1";

    public string SchemaVersion { get; init; } = ExportSchemaVersion;

    public DateTime GeneratedUtc { get; init; }

    public required EvolutionSimulationReportCandidateSection Candidate { get; init; }

    public required string PlanSnapshotJson { get; init; }

    public EvolutionSimulationPlanSnapshotPayload? PlanSnapshot { get; init; }

    public IReadOnlyList<EvolutionSimulationReportRunEntry> SimulationRuns { get; init; } = [];
}
