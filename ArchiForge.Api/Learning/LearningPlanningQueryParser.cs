using System.Globalization;

namespace ArchiForge.Api.Learning;

/// <summary>Parses bounded list parameters for <c>/v1/learning/*</c> GET endpoints.</summary>
internal static class LearningPlanningQueryParser
{
    public const int DefaultMaxItems = 50;

    public const int MinMaxItems = 1;

    public const int MaxMaxItems = 100;

    public static bool TryParseMaxItems(string? raw, string queryParameterName, out int value, out string? error) =>
        TryParseBoundedInt(
            raw,
            DefaultMaxItems,
            MinMaxItems,
            MaxMaxItems,
            queryParameterName,
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
