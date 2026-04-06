using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Contracts;

/// <summary>
/// Request body for <c>POST api/authority/replay</c>; maps to <see cref="ArchiForge.Persistence.Replay.ReplayRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "API contract DTO; no business logic.")]
public class ReplayRequestResponse
{
    /// <inheritdoc cref="ArchiForge.Persistence.Replay.ReplayRequest.RunId"/>
    public Guid RunId { get; set; }

    /// <inheritdoc cref="ArchiForge.Persistence.Replay.ReplayRequest.Mode"/>
    public string? Mode { get; set; }
}
