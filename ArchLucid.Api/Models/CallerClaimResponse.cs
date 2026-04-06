using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

/// <summary>Represents a single claim on the authenticated caller's principal.</summary>
[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class CallerClaimResponse
{
    /// <summary>The claim type URI or short name.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>The claim value.</summary>
    public string Value { get; init; } = string.Empty;
}
