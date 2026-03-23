namespace ArchiForge.Api.Contracts;

/// <summary>
/// JSON contract for <see cref="ArchiForge.Persistence.Replay.ReplayValidationResult"/>.
/// </summary>
public class ReplayValidationResponse
{
    /// <inheritdoc cref="ArchiForge.Persistence.Replay.ReplayValidationResult.ContextPresent"/>
    public bool ContextPresent
    {
        get; set;
    }
    /// <inheritdoc cref="ArchiForge.Persistence.Replay.ReplayValidationResult.GraphPresent"/>
    public bool GraphPresent
    {
        get; set;
    }

    /// <inheritdoc cref="ArchiForge.Persistence.Replay.ReplayValidationResult.FindingsPresent"/>
    public bool FindingsPresent
    {
        get; set;
    }

    /// <inheritdoc cref="ArchiForge.Persistence.Replay.ReplayValidationResult.ManifestPresent"/>
    public bool ManifestPresent
    {
        get; set;
    }

    /// <inheritdoc cref="ArchiForge.Persistence.Replay.ReplayValidationResult.TracePresent"/>
    public bool TracePresent
    {
        get; set;
    }

    /// <inheritdoc cref="ArchiForge.Persistence.Replay.ReplayValidationResult.ArtifactsPresent"/>
    public bool ArtifactsPresent
    {
        get; set;
    }

    /// <inheritdoc cref="ArchiForge.Persistence.Replay.ReplayValidationResult.ManifestHashMatches"/>
    public bool ManifestHashMatches
    {
        get; set;
    }

    /// <inheritdoc cref="ArchiForge.Persistence.Replay.ReplayValidationResult.ArtifactBundlePresentAfterReplay"/>
    public bool ArtifactBundlePresentAfterReplay
    {
        get; set;
    }

    /// <inheritdoc cref="ArchiForge.Persistence.Replay.ReplayValidationResult.Notes"/>
    public List<string> Notes { get; set; } = [];
}
