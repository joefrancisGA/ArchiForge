using System.ComponentModel.DataAnnotations;

using ArchiForge.Contracts.Common;

namespace ArchiForge.Contracts.Manifest;

/// <summary>
/// A resolved service node in a <see cref="GoldenManifest"/>, representing one deployable
/// service component in the target architecture.
/// </summary>
public sealed class ManifestService
{
    /// <summary>Unique service identifier used for relationship references within this manifest.</summary>
    [Required]
    public string ServiceId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Human-readable service name (e.g. <c>OrderService</c>, <c>Azure App Service</c>).</summary>
    [Required]
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>Functional category of this service (API, UI, Worker, etc.).</summary>
    [Required]
    public ServiceType ServiceType { get; set; }

    /// <summary>Deployment platform for this service (e.g. <c>AppService</c>, <c>ContainerApps</c>).</summary>
    [Required]
    public RuntimePlatform RuntimePlatform { get; set; }

    /// <summary>Short description of the service's role in the architecture. Optional.</summary>
    public string? Purpose { get; set; }

    /// <summary>Governance and security controls required for this specific service.</summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>Governance and security controls required for this specific service.</summary>
    public List<string> RequiredControls { get; set; } = [];
}
