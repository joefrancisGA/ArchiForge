using System.Diagnostics.CodeAnalysis;

namespace ArchLucid.Api.Models;

/// <summary>Represents the authenticated caller's identity and their associated claims.</summary>
[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class CallerIdentityResponse
{
    /// <summary>The caller's identity name derived from the authentication token.</summary>
    public string? Name
    {
        get;
        init;
    }

    /// <summary>The full list of claims present on the caller's principal.</summary>
    public IReadOnlyList<CallerClaimResponse> Claims
    {
        get;
        init;
    } = [];

    /// <summary>
    ///     When <see langword="true" />, the tenant scope has at least one committed architecture run with a golden
    ///     manifest — drives first-session nav narrowing in the operator UI only.
    /// </summary>
    public bool HasCommittedArchitectureReview
    {
        get;
        init;
    }
}
