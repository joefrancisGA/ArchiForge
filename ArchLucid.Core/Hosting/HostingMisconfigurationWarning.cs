namespace ArchLucid.Core.Hosting;

/// <summary>Structured production-like advisory pairing a TB-002 <c>rule_name</c> with operator-facing text.</summary>
public readonly record struct HostingMisconfigurationWarning(string RuleName, string Message);
