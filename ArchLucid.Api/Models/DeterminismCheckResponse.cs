using System.Diagnostics.CodeAnalysis;

using ArchiForge.Application.Determinism;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class DeterminismCheckResponse
{
    public DeterminismCheckResult Result { get; set; } = new();
}
