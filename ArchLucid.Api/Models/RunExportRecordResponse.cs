using System.Diagnostics.CodeAnalysis;

using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class RunExportRecordResponse
{
    public RunExportRecord Record { get; set; } = new();
}

