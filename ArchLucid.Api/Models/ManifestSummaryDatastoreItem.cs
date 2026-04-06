using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

/// <summary>Datastore entry within a <see cref="ManifestSummaryJsonResponse"/>.</summary>
[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class ManifestSummaryDatastoreItem
{
    public string Name { get; set; } = string.Empty;
    public string DatastoreType { get; set; } = string.Empty;
    public string RuntimePlatform { get; set; } = string.Empty;
    public string? Purpose { get; set; }
    public bool PrivateEndpointRequired { get; set; }
    public bool EncryptionAtRestRequired { get; set; }
}
