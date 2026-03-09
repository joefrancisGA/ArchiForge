using System.ComponentModel.DataAnnotations;
using ArchiForge.Contracts.Common;

namespace ArchiForge.Contracts.Manifest;

public sealed class ManifestService
{
    [Required]
    public string ServiceId { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    public string ServiceName { get; set; } = string.Empty;

    [Required]
    public ServiceType ServiceType { get; set; }

    [Required]
    public RuntimePlatform RuntimePlatform { get; set; }

    public string? Purpose { get; set; }

    public List<string> Tags { get; set; } = [];

    public List<string> RequiredControls { get; set; } = [];
}
