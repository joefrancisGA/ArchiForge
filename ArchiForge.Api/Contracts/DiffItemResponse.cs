namespace ArchiForge.Api.HttpContracts;

public class DiffItemResponse
{
    public string Section { get; set; } = null!;
    public string Key { get; set; } = null!;
    public string DiffKind { get; set; } = null!;
    public string? BeforeValue { get; set; }
    public string? AfterValue { get; set; }
    public string? Notes { get; set; }
}
