namespace ArchiForge.Api.Contracts;

/// <summary>
/// JSON subset of <see cref="ArchiForge.Persistence.Replay.ReplayResult"/> (omits full <see cref="ArchiForge.Persistence.Queries.RunDetailDto"/> and entity bodies).
/// </summary>
public class ReplayResponse
{
    /// <inheritdoc cref="ArchiForge.Persistence.Replay.ReplayResult.RunId"/>
    public Guid RunId { get; set; }

    /// <inheritdoc cref="ArchiForge.Persistence.Replay.ReplayResult.Mode"/>
    public string Mode { get; set; } = null!;

    /// <inheritdoc cref="ArchiForge.Persistence.Replay.ReplayResult.ReplayedUtc"/>
    public DateTime ReplayedUtc { get; set; }

    /// <summary>Id of <see cref="ArchiForge.Persistence.Replay.ReplayResult.RebuiltManifest"/> when present.</summary>
    public Guid? RebuiltManifestId { get; set; }

    /// <summary>Hash of rebuilt manifest when present.</summary>
    public string? RebuiltManifestHash { get; set; }

    /// <summary>Id of <see cref="ArchiForge.Persistence.Replay.ReplayResult.RebuiltArtifactBundle"/> when present.</summary>
    public Guid? RebuiltArtifactBundleId { get; set; }

    /// <inheritdoc cref="ArchiForge.Persistence.Replay.ReplayResult.Validation"/>
    public ReplayValidationResponse Validation { get; set; } = new();

    /// <summary>True when replay produced a rebuilt manifest or artifact bundle reference.</summary>
    public bool HasRebuildOutput { get; set; }

    /// <summary>Number of entries in <see cref="ReplayValidationResponse.Notes"/>.</summary>
    public int ValidationNoteCount { get; set; }
}
