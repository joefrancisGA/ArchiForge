using System.ComponentModel.DataAnnotations;
using ArchiForge.Contracts.Common;

namespace ArchiForge.Contracts.Requests;

public sealed class ArchitectureRequest
{
    [Required]
    public string RequestId { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    [MinLength(10)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string SystemName { get; set; } = string.Empty;

    [Required]
    public string Environment { get; set; } = "prod";

    [Required]
    public CloudProvider CloudProvider { get; set; } = CloudProvider.Azure;

    public List<string> Constraints { get; set; } = [];

    public List<string> RequiredCapabilities { get; set; } = [];

    public List<string> Assumptions { get; set; } = [];

    public string? PriorManifestVersion { get; set; }
}