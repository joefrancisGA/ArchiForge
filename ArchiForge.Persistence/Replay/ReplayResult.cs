using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Queries;

namespace ArchiForge.Persistence.Replay;

public class ReplayResult
{
    public Guid RunId { get; set; }
    public string Mode { get; set; } = default!;
    public DateTime ReplayedUtc { get; set; }

    public RunDetailDto Original { get; set; } = default!;

    public GoldenManifest? RebuiltManifest { get; set; }
    public ArtifactBundle? RebuiltArtifactBundle { get; set; }

    public ReplayValidationResult Validation { get; set; } = new();
}
