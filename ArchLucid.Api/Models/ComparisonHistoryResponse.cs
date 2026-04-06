using System.Diagnostics.CodeAnalysis;

using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class ComparisonHistoryResponse
{
    public List<ComparisonRecord> Records { get; set; } = [];
    public int? Limit { get; set; }
    public int? Skip { get; set; }
    public string? ComparisonType { get; set; }
    public string? LeftRunId { get; set; }
    public string? RightRunId { get; set; }
    public string? LeftExportRecordId { get; set; }
    public string? RightExportRecordId { get; set; }
    public string? Label { get; set; }
    public DateTime? CreatedFromUtc { get; set; }
    public DateTime? CreatedToUtc { get; set; }
    public string? Tag { get; set; }
    public List<string> Tags { get; set; } = [];
    public string? SortBy { get; set; }
    public string? SortDir { get; set; }
    public string? NextCursor { get; set; }
}

