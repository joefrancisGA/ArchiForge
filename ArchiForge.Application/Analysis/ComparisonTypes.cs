namespace ArchiForge.Application.Analysis;

/// <summary>
/// Well-known string constants used to identify comparison record types and payload formats.
/// </summary>
public static class ComparisonTypes
{
    /// <summary>End-to-end replay comparison between two architecture runs.</summary>
    public const string EndToEndReplay = "end-to-end-replay";

    /// <summary>Diff between two persisted export records.</summary>
    public const string ExportRecordDiff = "export-record-diff";

    /// <summary>Payload format that stores both a JSON blob and a Markdown summary.</summary>
    public const string FormatJsonMarkdown = "json+markdown";
}
