using System.Globalization;

using ArchiForge.Api.ProductLearning;

namespace ArchiForge.Api.Learning;

/// <summary>Parses bounded list parameters for <c>/v1/learning/*</c> GET endpoints.</summary>
internal static class LearningPlanningQueryParser
{
    public const int DefaultMaxItems = 50;

    public const int MinMaxItems = 1;

    public const int MaxMaxItems = 100;

    public const int DefaultMaxReportEvidenceRefsPerPlan = 100;

    public const int MinMaxReportEvidenceRefsPerPlan = 1;

    public const int MaxMaxReportEvidenceRefsPerPlan = 500;

    public static bool TryParseMaxItems(string? raw, string queryParameterName, out int value, out string? error) =>
        TryParseBoundedInt(
            raw,
            DefaultMaxItems,
            MinMaxItems,
            MaxMaxItems,
            queryParameterName,
            out value,
            out error);

    /// <summary>Reuses product-learning format rules (<c>markdown</c> / <c>json</c>).</summary>
    public static bool TryParseReportFormat(string? raw, out string formatNormalized, out string? error) =>
        ProductLearningQueryParser.TryParseReportFormat(raw, out formatNormalized, out error);

    public static bool TryParseMaxReportSignalLinksPerPlan(string? raw, out int value, out string? error) =>
        TryParseBoundedInt(
            raw,
            DefaultMaxReportEvidenceRefsPerPlan,
            MinMaxReportEvidenceRefsPerPlan,
            MaxMaxReportEvidenceRefsPerPlan,
            "maxReportSignalLinks",
            out value,
            out error);

    public static bool TryParseMaxReportArtifactLinksPerPlan(string? raw, out int value, out string? error) =>
        TryParseBoundedInt(
            raw,
            DefaultMaxReportEvidenceRefsPerPlan,
            MinMaxReportEvidenceRefsPerPlan,
            MaxMaxReportEvidenceRefsPerPlan,
            "maxReportArtifactLinks",
            out value,
            out error);

    public static bool TryParseMaxReportRunLinksPerPlan(string? raw, out int value, out string? error) =>
        TryParseBoundedInt(
            raw,
            DefaultMaxReportEvidenceRefsPerPlan,
            MinMaxReportEvidenceRefsPerPlan,
            MaxMaxReportEvidenceRefsPerPlan,
            "maxReportRunLinks",
            out value,
            out error);

    private static bool TryParseBoundedInt(
        string? raw,
        int defaultValue,
        int min,
        int max,
        string paramName,
        out int value,
        out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            value = defaultValue;
            return true;
        }

        if (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed))
        {
            value = 0;
            error = $"Query parameter '{paramName}' must be an integer.";
            return false;
        }

        if (parsed < min || parsed > max)
        {
            value = 0;
            error = $"Query parameter '{paramName}' must be between {min} and {max}.";
            return false;
        }

        value = parsed;
        return true;
    }
}
