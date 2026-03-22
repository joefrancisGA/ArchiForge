namespace ArchiForge.Decisioning.Alerts;

/// <summary>
/// <see cref="Evaluated"/> is the full set produced by rules this run (for digests).
/// <see cref="NewlyPersisted"/> excludes deduplicated open/acknowledged matches.
/// </summary>
public sealed record AlertEvaluationOutcome(
    IReadOnlyList<AlertRecord> Evaluated,
    IReadOnlyList<AlertRecord> NewlyPersisted);
