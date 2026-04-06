using System.ComponentModel.DataAnnotations;

using ArchiForge.Contracts.Common;

namespace ArchiForge.Contracts.Manifest;

/// <summary>
/// A resolved datastore node in a <see cref="GoldenManifest"/>, representing one
/// persistent storage component in the target architecture.
/// </summary>
public sealed class ManifestDatastore
{
    /// <summary>Unique datastore identifier used for relationship references within this manifest.</summary>
    [Required]
    public string DatastoreId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Human-readable datastore name (e.g. <c>OrdersDatabase</c>, <c>Azure SQL</c>).</summary>
    [Required]
    public string DatastoreName { get; set; } = string.Empty;

    /// <summary>Category of storage technology (relational, document, queue, blob, etc.).</summary>
    [Required]
    public DatastoreType DatastoreType { get; set; }

    /// <summary>Deployment platform for this datastore (e.g. <c>AzureSql</c>, <c>CosmosDb</c>).</summary>
    [Required]
    public RuntimePlatform RuntimePlatform { get; set; }

    /// <summary>Short description of the datastore's role in the architecture. Optional.</summary>
    public string? Purpose { get; set; }

    /// <summary>
    /// When <see langword="true"/>, the architecture decision requires this datastore
    /// to be accessed exclusively via a private endpoint.
    /// </summary>
    public bool PrivateEndpointRequired { get; set; }

    /// <summary>
    /// When <see langword="true"/>, all data persisted in this datastore must be
    /// encrypted at rest. Defaults to <see langword="true"/> as a secure baseline.
    /// </summary>
    public bool EncryptionAtRestRequired { get; set; } = true;
}
