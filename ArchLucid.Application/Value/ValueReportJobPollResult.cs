namespace ArchLucid.Application.Value;

public sealed record ValueReportJobPollResult(
    bool Found,
    bool Completed,
    byte[]? DocxBytes,
    string? FileName,
    string? ErrorMessage);
