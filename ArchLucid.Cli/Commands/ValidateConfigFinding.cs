namespace ArchLucid.Cli.Commands;

internal sealed record ValidateConfigFinding(
    ValidateConfigFindingSeverity Severity,
    string Category,
    string Check,
    string Detail);
