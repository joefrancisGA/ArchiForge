namespace ArchLucid.Contracts.Findings;

/// <summary>Shared severity scale for architecture findings (API contract + decisioning persistence).</summary>
public enum FindingSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}
