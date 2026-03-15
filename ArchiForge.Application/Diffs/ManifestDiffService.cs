using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Application.Diffs;

public sealed class ManifestDiffService : IManifestDiffService
{
    public ManifestDiffResult Compare(
        GoldenManifest left,
        GoldenManifest right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var result = new ManifestDiffResult
        {
            LeftManifestVersion = left.Metadata.ManifestVersion,
            RightManifestVersion = right.Metadata.ManifestVersion,
            AddedServices = GetAddedServiceNames(left, right),
            RemovedServices = GetRemovedServiceNames(left, right),
            AddedDatastores = GetAddedDatastoreNames(left, right),
            RemovedDatastores = GetRemovedDatastoreNames(left, right),
            AddedRequiredControls = GetAddedRequiredControls(left, right),
            RemovedRequiredControls = GetRemovedRequiredControls(left, right),
            AddedRelationships = GetAddedRelationships(left, right),
            RemovedRelationships = GetRemovedRelationships(left, right),
            Warnings = BuildWarnings(left, right)
        };

        return result;
    }

    private static List<string> GetAddedServiceNames(GoldenManifest left, GoldenManifest right)
    {
        var leftSet = left.Services
            .Select(s => s.ServiceName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return right.Services
            .Select(s => s.ServiceName)
            .Where(name => !leftSet.Contains(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    private static List<string> GetRemovedServiceNames(GoldenManifest left, GoldenManifest right)
    {
        var rightSet = right.Services
            .Select(s => s.ServiceName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return left.Services
            .Select(s => s.ServiceName)
            .Where(name => !rightSet.Contains(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    private static List<string> GetAddedDatastoreNames(GoldenManifest left, GoldenManifest right)
    {
        var leftSet = left.Datastores
            .Select(d => d.DatastoreName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return right.Datastores
            .Select(d => d.DatastoreName)
            .Where(name => !leftSet.Contains(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    private static List<string> GetRemovedDatastoreNames(GoldenManifest left, GoldenManifest right)
    {
        var rightSet = right.Datastores
            .Select(d => d.DatastoreName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return left.Datastores
            .Select(d => d.DatastoreName)
            .Where(name => !rightSet.Contains(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    private static List<string> GetAddedRequiredControls(GoldenManifest left, GoldenManifest right)
    {
        var leftSet = left.Governance.RequiredControls
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return right.Governance.RequiredControls
            .Where(control => !leftSet.Contains(control))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    private static List<string> GetRemovedRequiredControls(GoldenManifest left, GoldenManifest right)
    {
        var rightSet = right.Governance.RequiredControls
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return left.Governance.RequiredControls
            .Where(control => !rightSet.Contains(control))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    private static List<RelationshipDiffItem> GetAddedRelationships(GoldenManifest left, GoldenManifest right)
    {
        var leftSet = left.Relationships
            .Select(ToRelationshipKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return right.Relationships
            .Where(r => !leftSet.Contains(ToRelationshipKey(r)))
            .Select(ToDiffItem)
            .OrderBy(r => r.SourceId)
            .ThenBy(r => r.TargetId)
            .ThenBy(r => r.RelationshipType)
            .ToList();
    }

    private static List<RelationshipDiffItem> GetRemovedRelationships(GoldenManifest left, GoldenManifest right)
    {
        var rightSet = right.Relationships
            .Select(ToRelationshipKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return left.Relationships
            .Where(r => !rightSet.Contains(ToRelationshipKey(r)))
            .Select(ToDiffItem)
            .OrderBy(r => r.SourceId)
            .ThenBy(r => r.TargetId)
            .ThenBy(r => r.RelationshipType)
            .ToList();
    }

    private static string ToRelationshipKey(ManifestRelationship relationship)
    {
        return $"{relationship.SourceId}|{relationship.TargetId}|{relationship.RelationshipType}";
    }

    private static RelationshipDiffItem ToDiffItem(ManifestRelationship relationship)
    {
        return new RelationshipDiffItem
        {
            SourceId = relationship.SourceId,
            TargetId = relationship.TargetId,
            RelationshipType = relationship.RelationshipType.ToString(),
            Description = relationship.Description
        };
    }

    private static List<string> BuildWarnings(GoldenManifest left, GoldenManifest right)
    {
        var warnings = new List<string>();

        if (!string.Equals(left.SystemName, right.SystemName, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("SystemName differs between compared manifests.");
        }

        if (!string.Equals(left.RunId, right.RunId, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("RunId differs between compared manifests.");
        }

        return warnings;
    }
}
