using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Api.Models;

public sealed class ComparisonHistoryResponse
{
    public List<ComparisonRecord> Records { get; set; } = [];
}

