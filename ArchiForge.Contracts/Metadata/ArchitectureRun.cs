using System.ComponentModel.DataAnnotations;
using ArchiForge.Contracts.Common;

namespace ArchiForge.Contracts.Metadata;

public sealed class ArchitectureRun
{
    [Required]
    public string RunId { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    public string RequestId { get; set; } = string.Empty;

    [Required]
    public ArchitectureRunStatus Status { get; set; } = ArchitectureRunStatus.Created;

    [Required]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedUtc { get; set; }

    public string? CurrentManifestVersion { get; set; }

    public List<string> TaskIds { get; set; } = [];
}