using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Api.Models;

public sealed class ComparisonHistoryResponse
{
    public List<ComparisonRecord> Records { get; set; } = [];

    public int? Limit { get; set; }

    public int? Skip { get; set; }

    public string? ComparisonType { get; set; }

    public string? LeftRunId { get; set; }

    public string? RightRunId { get; set; }

    public DateTime? CreatedFromUtc { get; set; }

    public DateTime? CreatedToUtc { get; set; }

    public string? Tag { get; set; }
}

