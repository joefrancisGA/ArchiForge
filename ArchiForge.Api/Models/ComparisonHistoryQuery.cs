namespace ArchiForge.Api.Models;

/// <summary>Query parameters for GET /v1/architecture/comparisons search.</summary>
public sealed class ComparisonHistoryQuery
{
    public string? ComparisonType
    {
        get; set;
    }

    public string? LeftRunId
    {
        get; set;
    }

    public string? RightRunId
    {
        get; set;
    }

    public string? LeftExportRecordId
    {
        get; set;
    }

    public string? RightExportRecordId
    {
        get; set;
    }

    public string? Label
    {
        get; set;
    }

    public DateTime? CreatedFromUtc
    {
        get; set;
    }

    public DateTime? CreatedToUtc
    {
        get; set;
    }

    public string? Tag
    {
        get; set;
    }

    public string[]? Tags
    {
        get; set;
    }

    public string? SortBy { get; set; } = "createdUtc";

    public string? SortDir { get; set; } = "desc";

    public string? Cursor
    {
        get; set;
    }

    public int Skip
    {
        get; set;
    }

    /// <summary>Page size; 0 or omitted means 50 (max 500).</summary>
    public int Limit
    {
        get; set;
    }

    /// <summary>Merges <see cref="Tag"/> and <see cref="Tags"/> into a distinct list.</summary>
    public static List<string> NormalizeTagList(string? tag, string[]? tags)
    {
        var normalizedTags = (tags ?? [])
            .SelectMany(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (string.IsNullOrWhiteSpace(tag))
            return normalizedTags;

        normalizedTags.AddRange(tag.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        normalizedTags = normalizedTags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return normalizedTags;
    }
}
