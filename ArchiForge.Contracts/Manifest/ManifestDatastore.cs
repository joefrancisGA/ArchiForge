using System.ComponentModel.DataAnnotations;
using ArchiForge.Contracts.Common;

namespace ArchiForge.Contracts.Manifest;

public sealed class ManifestDatastore
{
    [Required]
    public string DatastoreId { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    public string DatastoreName { get; set; } = string.Empty;

    [Required]
    public DatastoreType DatastoreType { get; set; }

    [Required]
    public RuntimePlatform RuntimePlatform { get; set; }

    public string? Purpose { get; set; }

    public bool PrivateEndpointRequired { get; set; }

    public bool EncryptionAtRestRequired { get; set; } = true;
}