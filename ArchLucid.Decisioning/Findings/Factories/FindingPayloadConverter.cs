using System.Text.Json;

using ArchiForge.Decisioning.Findings.Payloads;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Findings.Factories;

/// <summary>
/// Converts a <see cref="Finding.Payload"/> object to a strongly-typed payload DTO.
/// </summary>
/// <remarks>
/// Three payload shapes are handled in priority order:
/// <list type="number">
///   <item>Already the target type — returned directly with no allocation.</item>
///   <item><see cref="JsonElement"/> — deserialized using <see cref="CaseInsensitiveOptions"/>.</item>
///   <item>Any other <see cref="object"/> — round-tripped through JSON.</item>
/// </list>
/// </remarks>
public static class FindingPayloadConverter
{
    /// <summary>
    /// Shared JSON options used for payload deserialization.
    /// Case-insensitive matching is required to handle mixed-case keys returned by LLM engines.
    /// </summary>
    private static readonly JsonSerializerOptions CaseInsensitiveOptions =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Converts <see cref="Finding.Payload"/> to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The target payload type.</typeparam>
    /// <param name="finding">The finding whose payload should be converted.</param>
    /// <returns>
    /// The converted payload, or <see langword="default"/> when <see cref="Finding.Payload"/> is
    /// <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="finding"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails.</exception>
    public static T? ConvertPayload<T>(Finding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);

        if (finding.Payload is null)
            return default;

        if (finding.Payload is T typed)
            return typed;

        if (finding.Payload is JsonElement jsonElement)
        
            try
            {
                return jsonElement.Deserialize<T>(CaseInsensitiveOptions);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Finding payload cannot be deserialized as {typeof(T).Name} (FindingId={finding.FindingId}).", ex);
            }
        

        try
        {
            string json = JsonSerializer.Serialize(finding.Payload);
            return JsonSerializer.Deserialize<T>(json, CaseInsensitiveOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Finding payload cannot be serialized/deserialized as {typeof(T).Name} (FindingId={finding.FindingId}).", ex);
        }
    }

    /// <summary>Converts the payload to <see cref="RequirementFindingPayload"/>.</summary>
    public static RequirementFindingPayload? ToRequirementPayload(Finding finding)
        => ConvertPayload<RequirementFindingPayload>(finding);

    /// <summary>Converts the payload to <see cref="TopologyGapFindingPayload"/>.</summary>
    public static TopologyGapFindingPayload? ToTopologyGapPayload(Finding finding)
        => ConvertPayload<TopologyGapFindingPayload>(finding);

    /// <summary>Converts the payload to <see cref="SecurityControlFindingPayload"/>.</summary>
    public static SecurityControlFindingPayload? ToSecurityControlPayload(Finding finding)
        => ConvertPayload<SecurityControlFindingPayload>(finding);

    /// <summary>Converts the payload to <see cref="CostConstraintFindingPayload"/>.</summary>
    public static CostConstraintFindingPayload? ToCostConstraintPayload(Finding finding)
        => ConvertPayload<CostConstraintFindingPayload>(finding);

    /// <summary>Converts the payload to <see cref="PolicyApplicabilityFindingPayload"/>.</summary>
    public static PolicyApplicabilityFindingPayload? ToPolicyApplicabilityPayload(Finding finding)
        => ConvertPayload<PolicyApplicabilityFindingPayload>(finding);

    /// <summary>Converts the payload to <see cref="TopologyCoverageFindingPayload"/>.</summary>
    public static TopologyCoverageFindingPayload? ToTopologyCoveragePayload(Finding finding)
        => ConvertPayload<TopologyCoverageFindingPayload>(finding);

    /// <summary>Converts the payload to <see cref="SecurityCoverageFindingPayload"/>.</summary>
    public static SecurityCoverageFindingPayload? ToSecurityCoveragePayload(Finding finding)
        => ConvertPayload<SecurityCoverageFindingPayload>(finding);

    /// <summary>Converts the payload to <see cref="PolicyCoverageFindingPayload"/>.</summary>
    public static PolicyCoverageFindingPayload? ToPolicyCoveragePayload(Finding finding)
        => ConvertPayload<PolicyCoverageFindingPayload>(finding);

    /// <summary>Converts the payload to <see cref="RequirementCoverageFindingPayload"/>.</summary>
    public static RequirementCoverageFindingPayload? ToRequirementCoveragePayload(Finding finding)
        => ConvertPayload<RequirementCoverageFindingPayload>(finding);

    /// <summary>Converts the payload to <see cref="ComplianceFindingPayload"/>.</summary>
    public static ComplianceFindingPayload? ToCompliancePayload(Finding finding)
        => ConvertPayload<ComplianceFindingPayload>(finding);
}
