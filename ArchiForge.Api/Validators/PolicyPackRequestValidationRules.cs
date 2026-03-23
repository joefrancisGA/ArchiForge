using System.Text.Json;
using System.Text.RegularExpressions;

using ArchiForge.Decisioning.Governance.PolicyPacks;

namespace ArchiForge.Api.Validators;

/// <summary>
/// Shared predicates for policy pack HTTP request validation (SemVer labels, JSON shape, pack type whitelist).
/// </summary>
/// <remarks>
/// Consumed by <see cref="CreatePolicyPackRequestValidator"/>, <see cref="PublishPolicyPackVersionRequestValidator"/>, and
/// <see cref="AssignPolicyPackRequestValidator"/>. Keeps regex and JSON parsing in one place so API and tests stay aligned.
/// </remarks>
public static class PolicyPackRequestValidationRules
{
    /// <summary>SemVer 2-style <c>MAJOR.MINOR.PATCH</c> with optional pre-release / build; optional leading <c>v</c>.</summary>
    private static readonly Regex PolicyPackVersionSemVer = new(
        @"^v?(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>
    /// Returns whether <paramref name="value"/> matches the policy pack version pattern (trimmed).
    /// </summary>
    /// <param name="value">Raw version string from assign or publish body.</param>
    /// <returns><c>false</c> for null/whitespace; otherwise regex match result.</returns>
    /// <remarks>
    /// Slightly stricter than full SemVer grammar in edge cases; aligned with integration tests and problem-detail messages.
    /// </remarks>
    public static bool BePolicyPackSemVerVersion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        return PolicyPackVersionSemVer.IsMatch(value.Trim());
    }

    /// <summary>
    /// Returns <c>true</c> for null/whitespace, or for text that parses as a single JSON value/object/array.
    /// </summary>
    /// <param name="value">JSON string from create/publish bodies.</param>
    /// <remarks>Empty string is treated as valid (caller may normalize to <c>{}</c> downstream).</remarks>
    public static bool BeValidJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        try
        {
            JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>Allowed <see cref="CreatePolicyPackRequest.PackType"/> values (case-insensitive set).</summary>
    public static readonly HashSet<string> ValidPackTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        PolicyPackType.BuiltIn,
        PolicyPackType.TenantCustom,
        PolicyPackType.WorkspaceCustom,
        PolicyPackType.ProjectCustom,
    };
}
