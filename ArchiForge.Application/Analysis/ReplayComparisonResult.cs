namespace ArchiForge.Application.Analysis;

public sealed class ReplayComparisonResult
{
    public string ComparisonRecordId { get; set; } = string.Empty;

    public string ComparisonType { get; set; } = string.Empty;

    public string Format { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string? Content { get; set; }

    public byte[]? BinaryContent { get; set; }
}

