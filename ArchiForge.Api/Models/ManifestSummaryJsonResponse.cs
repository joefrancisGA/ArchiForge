namespace ArchiForge.Api.Models;

/// <summary>Top-level manifest summary returned by the manifest summary JSON endpoint.</summary>
public sealed class ManifestSummaryJsonResponse
{
    public string ManifestVersion { get; set; } = string.Empty;

    public string SystemName { get; set; } = string.Empty;

    public int ServiceCount { get; set; }

    public int DatastoreCount { get; set; }

    public int RelationshipCount { get; set; }

    public List<string> RequiredControls { get; set; } = [];

    public List<ManifestSummaryServiceItem> Services { get; set; } = [];

    public List<ManifestSummaryDatastoreItem> Datastores { get; set; } = [];

    public List<ManifestSummaryRelationshipItem> Relationships { get; set; } = [];
}
