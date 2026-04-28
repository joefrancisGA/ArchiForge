namespace ArchLucid.Application;

public sealed record SeedFakeResultsResult(
    bool Success,
    int ResultCount,
    string? Error,
    ApplicationServiceFailureKind? FailureKind = null);
