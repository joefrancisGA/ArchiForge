using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArchLucid.Core.Explanation;

/// <summary>
///     Parses LLM output into <see cref="StructuredExplanation" />; never throws. Non-JSON or invalid payloads become a
///     fallback envelope.
/// </summary>
public static class StructuredExplanationParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    ///     Returns <see langword="true" /> when <paramref name="rawText" /> is JSON that deserializes to a non-empty
    ///     <c>reasoning</c> field.
    /// </summary>
    public static bool TryNormalizeStructuredJson(
        string? rawText,
        [NotNullWhen(true)] out StructuredExplanation? structured)
    {
        structured = null;

        if (string.IsNullOrWhiteSpace(rawText))
            return false;

        try
        {
            StructuredExplanationDto? dto =
                JsonSerializer.Deserialize<StructuredExplanationDto>(rawText.Trim(), Options);

            if (dto is null || string.IsNullOrWhiteSpace(dto.Reasoning))
                return false;

            structured = MapFromDto(dto);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    ///     Produces a <see cref="StructuredExplanation" /> from LLM output. On failure, returns an envelope with
    ///     <see cref="StructuredExplanation.Reasoning" /> set to the trimmed raw text (may be empty).
    /// </summary>
    public static StructuredExplanation Parse(string? rawText)
    {
        if (TryNormalizeStructuredJson(rawText, out StructuredExplanation? normalized))
            return normalized;

        return new StructuredExplanation
        {
            Reasoning = rawText?.Trim() ?? string.Empty,
            SchemaVersion = 1,
            EvidenceRefs = [],
            Confidence = null,
            AlternativesConsidered = null,
            Caveats = null
        };
    }

    internal static decimal? ClampConfidence(decimal? value)
    {
        if (value is null)
            return null;

        decimal v = value.Value;

        if (v < 0m)
            return 0m;

        if (v > 1m)
            return 1m;

        return v;
    }

    private static StructuredExplanation MapFromDto(StructuredExplanationDto dto)
    {
        int version = dto.SchemaVersion <= 0 ? 1 : dto.SchemaVersion;

        return new StructuredExplanation
        {
            SchemaVersion = version,
            Reasoning = dto.Reasoning!.Trim(),
            EvidenceRefs = dto.EvidenceRefs ?? [],
            Confidence = ClampConfidence(dto.Confidence),
            AlternativesConsidered = dto.AlternativesConsidered,
            Caveats = dto.Caveats
        };
    }

    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    private sealed class StructuredExplanationDto
    {
        public int SchemaVersion
        {
            get;
            set;
        } = 1;

        public string? Reasoning
        {
            get;
            set;
        }

        public List<string>? EvidenceRefs
        {
            get;
            set;
        }

        public decimal? Confidence
        {
            get;
            set;
        }

        public List<string>? AlternativesConsidered
        {
            get;
            set;
        }

        public List<string>? Caveats
        {
            get;
            set;
        }
    }
}
