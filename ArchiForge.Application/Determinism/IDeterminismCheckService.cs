namespace ArchiForge.Application.Determinism;

public interface IDeterminismCheckService
{
    Task<DeterminismCheckResult> RunAsync(
        DeterminismCheckRequest request,
        CancellationToken cancellationToken = default);
}
