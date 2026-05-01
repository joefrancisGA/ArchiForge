namespace ArchLucid.Application.Determinism;

/// <summary>
///     Runs a determinism check by replaying an architecture run multiple times and comparing
///     agent results and manifest output across iterations.
/// </summary>
public interface IDeterminismCheckService
{
    /// <summary>
    ///     Executes the determinism check described by <paramref name="request" /> and returns a
    ///     <see cref="DeterminismCheckResult" /> summarising per-iteration drift.
    /// </summary>
    /// <exception cref="System.ArgumentException">Thrown when <see cref="DeterminismCheckRequest.RunId" /> is blank.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    ///     Thrown when <see cref="DeterminismCheckRequest.Iterations" /> is
    ///     less than 2.
    /// </exception>
    Task<DeterminismCheckResult> RunAsync(
        DeterminismCheckRequest request,
        CancellationToken cancellationToken = default);
}
