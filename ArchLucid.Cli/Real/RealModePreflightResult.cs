namespace ArchLucid.Cli.Real;

internal sealed record RealModePreflightResult(bool IsOk, IReadOnlyList<string> MissingKeys, string? ErrorMessage);
