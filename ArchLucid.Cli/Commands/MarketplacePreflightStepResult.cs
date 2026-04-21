namespace ArchLucid.Cli.Commands;

/// <summary>One row in the <c>archlucid marketplace preflight</c> report.</summary>
public sealed record MarketplacePreflightStepResult(string Id, bool Passed, string Detail);
