using ArchLucid.Contracts.Evolution;

namespace ArchLucid.Contracts.Abstractions.Evolution;

/// <summary>
///     Runs one or two read-only architecture analysis passes to simulate evaluating a <see cref="CandidateChangeSet" />
///     against a baseline run
///     without replay commits, determinism iterations, or manifest writes.
/// </summary>
public interface ISimulationEngine
{
    /// <summary>
    ///     Builds <see cref="SimulationResult" /> with optional before/after diff using read-only architecture analysis only.
    /// </summary>
    Task<SimulationResult> SimulateAsync(SimulationRequest request, CancellationToken cancellationToken = default);
}
