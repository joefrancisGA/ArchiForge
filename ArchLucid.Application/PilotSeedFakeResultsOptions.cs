namespace ArchLucid.Application;

/// <summary>Optional flags for development-only <see cref="IArchitectureApplicationService.SeedFakeResultsAsync" />.</summary>
public sealed record PilotSeedFakeResultsOptions(bool MarkRealModeFellBackToSimulator);
