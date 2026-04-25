using ArchLucid.Decisioning.Interfaces;

namespace ArchLucid.Persistence.Coordination.Replay;

/// <summary>
///     Reconstructs and optionally re-executes decisioning for a persisted authority run (integrity checks, new
///     manifest/trace, optional artifacts).
/// </summary>
/// <remarks>
///     Implementation: <see cref="AuthorityReplayService" />. HTTP:
///     <c>ArchLucid.Api.Controllers.AuthorityReplayController</c> (maps request/response DTOs).
/// </remarks>
public interface IAuthorityReplayService
{
    /// <summary>
    ///     Loads the run via <see cref="Queries.IAuthorityQueryService.GetRunDetailAsync" /> using the caller’s current scope,
    ///     then applies <see cref="ReplayRequest.Mode" />.
    /// </summary>
    /// <param name="request">Target run and mode string (see <see cref="ReplayMode" />).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation notes and optional rebuilt entities, or <see langword="null" /> when the run is not found.</returns>
    /// <remarks>
    ///     <see cref="ReplayMode.RebuildManifest" /> re-runs <see cref="IDecisionEngine.DecideAsync" /> and persists a new
    ///     trace/manifest; <see cref="ReplayMode.RebuildArtifacts" /> additionally synthesizes and saves an artifact bundle.
    /// </remarks>
    Task<ReplayResult?> ReplayAsync(
        ReplayRequest request,
        CancellationToken ct);
}
