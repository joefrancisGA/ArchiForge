using System.Diagnostics.CodeAnalysis;

namespace ArchLucid.Api.Models;

/// <summary>
/// Ingest payload from the operator shell when the browser reports an unexpected client error (log-only sink).
/// </summary>
[ExcludeFromCodeCoverage(Justification = "API request DTO; validation exercised via controller tests.")]
public sealed class ClientErrorReport
{
    public string Message { get; set; } = string.Empty;

    public string? Stack { get; set; }

    public string? Pathname { get; set; }

    public string? UserAgent { get; set; }

    public string? TimestampUtc { get; set; }

    public Dictionary<string, string>? Context { get; set; }
}
