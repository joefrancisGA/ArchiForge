namespace ArchiForge.Application.Analysis;

public sealed class DriftAnalysisResult
{
    public bool DriftDetected { get; set; }

    public List<DriftItem> Items { get; set; } = new();

    public string Summary { get; set; } = string.Empty;
}

public sealed class DriftItem
{
    public string Category { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public string? StoredValue { get; set; }

    public string? RegeneratedValue { get; set; }

    public string Description { get; set; } = string.Empty;
}
