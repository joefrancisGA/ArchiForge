using System.Text.Json;
using System.Text.RegularExpressions;
using ArchiForge.Decisioning.Governance.PolicyPacks;

namespace ArchiForge.Api.Validators;

public static class PolicyPackRequestValidationRules
{
    /// <summary>SemVer 2-style <c>MAJOR.MINOR.PATCH</c> with optional pre-release / build; optional leading <c>v</c>.</summary>
    private static readonly Regex PolicyPackVersionSemVer = new(
        @"^v?(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool BePolicyPackSemVerVersion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        return PolicyPackVersionSemVer.IsMatch(value.Trim());
    }

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

    public static readonly HashSet<string> ValidPackTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        PolicyPackType.BuiltIn,
        PolicyPackType.TenantCustom,
        PolicyPackType.WorkspaceCustom,
        PolicyPackType.ProjectCustom,
    };
}
