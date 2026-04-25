using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Queries;

namespace ArchLucid.Persistence.Coordination.Replay;

/// <summary>
///     Outcome of a replay: original hydrated run, optional rebuilt manifest/bundle, and validation flags/notes.
/// </summary>
/// <remarks>Mapped to <c>ArchLucid.Api.Contracts.ReplayResponse</c> for HTTP (subset of fields).</remarks>
public class ReplayResult
{
    /// <summary>The authority run that was replayed.</summary>
    public Guid RunId
    {
        get;
        set;
    }

    /// <summary>Effective mode after normalization.</summary>
    public string Mode
    {
        get;
        set;
    } = null!;

    /// <summary>When replay finished (UTC).</summary>
    public DateTime ReplayedUtc
    {
        get;
        set;
    }

    /// <summary>Snapshot of the authority run as loaded before rebuild steps.</summary>
    public RunDetailDto Original
    {
        get;
        set;
    } = null!;

    /// <summary>New manifest when rebuild path ran; otherwise <see langword="null" />.</summary>
    public GoldenManifest? RebuiltManifest
    {
        get;
        set;
    }

    /// <summary>
    ///     New bundle when <see cref="ReplayMode.RebuildArtifacts" /> completed synthesis; otherwise
    ///     <see langword="null" />.
    /// </summary>
    public ArtifactBundle? RebuiltArtifactBundle
    {
        get;
        set;
    }

    /// <summary>Presence checks, hash comparison, and human-readable notes.</summary>
    public ReplayValidationResult Validation
    {
        get;
        set;
    } = new();
}
