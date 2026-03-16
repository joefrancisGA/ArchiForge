using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Api.Models;

public sealed class RunExportHistoryResponse
{
    public List<RunExportRecord> Exports { get; set; } = [];
}

