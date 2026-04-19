using System.Globalization;

namespace ArchLucid.Api.ProductLearning;

/// <summary>Parses and validates shared query parameters for product-learning GET endpoints.</summary>
internal static class ProductLearningQueryParser
{
    public const int DefaultMaxImprovementOpportunities = 12;

    public const int MinMaxImprovementOpportunities = 1;

    public const int MaxMaxImprovementOpportunities = 50;

    public const int DefaultMaxTriageQueueItems = 25;

    public const int MinMaxTriageQueueItems = 1;

    public const int MaxMaxTriageQueueItems = 100;

    public const int DefaultMaxReportArtifacts = 10;

    public const int MinMaxReportArtifacts = 1;

    public const int MaxMaxReportArtifacts = 25;

    public const int DefaultMaxReportImprovements = 10;

    public const int MinMaxReportImprovements = 1;

    public const int MaxMaxReportImprovements = 20;

    public const int DefaultMaxReportTriagePreview = 15;

    public const int MinMaxReportTriagePreview = 1;

    public const int MaxMaxReportTriagePreview = 25;

    /// <summary><paramref name="formatNormalized"/> is <c>markdown</c> or <c>json</c> (lowercase).</summary>
    public static bool TryParseReportFormat(string? raw, out string formatNormalized, out string? error)
    {
        formatNormalized = "markdown";
        error = null;

        if (string.IsNullOrWhiteSpace(raw))
            return true;


        string f = raw.Trim();

        if (string.Equals(f, "markdown", StringComparison.OrdinalIgnoreCase))
        {
            formatNormalized = "markdown";
            return true;
        }

        if (string.Equals(f, "json", StringComparison.OrdinalIgnoreCase))
        {
            formatNormalized = "json";
            return true;
        }

        error = "Query parameter 'format' must be 'markdown' or 'json'.";
        return false;
    }

    public static bool TryParseMaxReportArtifacts(string? raw, out int value, out string? error) =>
        TryParseBoundedInt(
            raw,
            DefaultMaxReportArtifacts,
            MinMaxReportArtifacts,
            MaxMaxReportArtifacts,
            "maxReportArtifacts",
            out value,
            out error);

    public static bool TryParseMaxReportImprovements(string? raw, out int value, out string? error) =>
        TryParseBoundedInt(
            raw,
            DefaultMaxReportImprovements,
            MinMaxReportImprovements,
            MaxMaxReportImprovements,
            "maxReportImprovements",
            out value,
            out error);

    public static bool TryParseMaxReportTriagePreview(string? raw, out int value, out string? error) =>
        TryParseBoundedInt(
            raw,
            DefaultMaxReportTriagePreview,
            MinMaxReportTriagePreview,
            MaxMaxReportTriagePreview,
            "maxReportTriage",
            out value,
            out error);

    /// <summary>Empty or whitespace <paramref name="since"/> yields <c>null</c> (all time).</summary>
    public static bool TryParseOptionalSince(string? since, out DateTime? sinceUtc, out string? error)
    {
        sinceUtc = null;
        error = null;

        if (string.IsNullOrWhiteSpace(since))
            return true;


        if (!DateTimeOffset.TryParse(
                since,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind | DateTimeStyles.AssumeUniversal,
                out DateTimeOffset parsed))
        {
            error = "Query parameter 'since' must be a valid ISO 8601 date-time (use UTC or include offset).";
            return false;
        }

        sinceUtc = parsed.UtcDateTime;
        return true;
    }

    public static bool TryParseMaxImprovementOpportunities(string? raw, out int value, out string? error) =>
        TryParseBoundedInt(
            raw,
            DefaultMaxImprovementOpportunities,
            MinMaxImprovementOpportunities,
            MaxMaxImprovementOpportunities,
            "maxOpportunities",
            out value,
            out error);

    public static bool TryParseMaxTriageQueueItems(string? raw, out int value, out string? error) =>
        TryParseBoundedInt(
            raw,
            DefaultMaxTriageQueueItems,
            MinMaxTriageQueueItems,
            MaxMaxTriageQueueItems,
            "maxTriageItems",
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
