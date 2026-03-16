using ArchiForge.Application.Analysis;

namespace ArchiForge.Api.Models;

public sealed class ExportRecordDiffResponse
{
    public ExportRecordDiffResult Diff { get; set; } = new();
}

