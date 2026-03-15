using ArchiForge.Application.Determinism;

namespace ArchiForge.Api.Models;

public sealed class DeterminismCheckResponse
{
    public DeterminismCheckResult Result { get; set; } = new();
}
