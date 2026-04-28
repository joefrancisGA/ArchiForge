namespace ArchLucid.Application;

public sealed record SubmitResultResult(
    bool Success,
    string? ResultId,
    string? Error,
    ApplicationServiceFailureKind? FailureKind = null);
