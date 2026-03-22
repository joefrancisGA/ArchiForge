using System.Text;
using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Application.Diagrams;

public sealed class ManifestDiagramService : IManifestDiagramService
{
    public string GenerateMermaid(GoldenManifest manifest, ManifestDiagramOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        options ??= new ManifestDiagramOptions();

        var layout = NormalizeLayout(options.Layout);
        var relationshipLabels = NormalizeRelationshipLabels(options.RelationshipLabels);
        var groupBy = NormalizeGroupBy(options.GroupBy);

        var sb = new StringBuilder();
        sb.AppendLine($"flowchart {layout}");

        // Build stable, collision-safe node IDs for services and datastores.
        var nodeIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var usedNodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Optional grouping (subgraphs) for services only.
        if (manifest.Services.Count > 0)
        {
            if (groupBy == "none")
            {
                foreach (var service in manifest.Services.OrderBy(s => s.ServiceName, StringComparer.OrdinalIgnoreCase))
                {
                    var nodeId = GetOrCreateNodeId("svc", service.ServiceId, service.ServiceName);
                    var label = BuildServiceLabel(service, options.IncludeRuntimePlatform);
                    sb.AppendLine($"    {nodeId}[\"{EscapeLabel(label)}\"]");
                }
            }
            else
            {
                var groups = manifest.Services
                    .GroupBy(s => groupBy == "runtimeplatform"
                        ? (s.RuntimePlatform.ToString())
                        : (s.ServiceType.ToString()), StringComparer.OrdinalIgnoreCase)
                    .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

                foreach (var g in groups)
                {
                    sb.AppendLine($"    subgraph {SanitizeId(g.Key)}[\"{EscapeLabel(g.Key)}\"]");
                    foreach (var service in g.OrderBy(s => s.ServiceName, StringComparer.OrdinalIgnoreCase))
                    {
                        var nodeId = GetOrCreateNodeId("svc", service.ServiceId, service.ServiceName);
                        var label = BuildServiceLabel(service, options.IncludeRuntimePlatform);
                        sb.AppendLine($"        {nodeId}[\"{EscapeLabel(label)}\"]");
                    }
                    sb.AppendLine("    end");
                }
            }
        }

        foreach (var datastore in manifest.Datastores.OrderBy(d => d.DatastoreName, StringComparer.OrdinalIgnoreCase))
        {
            var nodeId = GetOrCreateNodeId("ds", datastore.DatastoreId, datastore.DatastoreName);
            var label = BuildDatastoreLabel(datastore, options.IncludeRuntimePlatform);
            sb.AppendLine($"    {nodeId}[(\"{EscapeLabel(label)}\")]");
        }

        if (manifest.Services.Count > 0 || manifest.Datastores.Count > 0)
            sb.AppendLine();

        foreach (var relationship in manifest.Relationships)
        {
            var source = ResolveExistingNodeId(relationship.SourceId, manifest, nodeIds)
                         ?? SanitizeId(relationship.SourceId);
            var target = ResolveExistingNodeId(relationship.TargetId, manifest, nodeIds)
                         ?? SanitizeId(relationship.TargetId);

            // Skip edges with missing endpoints.
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
                continue;

            if (relationshipLabels == "none")
            {
                sb.AppendLine($"    {source} --> {target}");
                continue;
            }

            var edgeLabel = EscapeLabel(relationship.RelationshipType.ToString());
            sb.AppendLine($"    {source} -->|{edgeLabel}| {target}");
        }

        return sb.ToString().TrimEnd();

        string GetOrCreateNodeId(string kind, string rawId, string fallbackName)
        {
            var key = $"{kind}:{rawId}";
            if (!string.IsNullOrWhiteSpace(rawId) && nodeIds.TryGetValue(key, out var existing))
                return existing;

            var baseId = SanitizeId(string.IsNullOrWhiteSpace(rawId) ? fallbackName : rawId);
            var unique = EnsureUnique(baseId, usedNodeIds);

            if (!string.IsNullOrWhiteSpace(rawId))
                nodeIds[key] = unique;

            return unique;
        }
    }

    private static string? ResolveExistingNodeId(
        string sourceOrTargetId,
        GoldenManifest manifest,
        Dictionary<string, string> nodeIds)
    {
        if (string.IsNullOrWhiteSpace(sourceOrTargetId))
            return null;

        var svc = manifest.Services.FirstOrDefault(s =>
            s.ServiceId.Equals(sourceOrTargetId, StringComparison.OrdinalIgnoreCase));

        if (svc is not null)
        {
            var key = $"svc:{svc.ServiceId}";
            if (!string.IsNullOrWhiteSpace(svc.ServiceId) && nodeIds.TryGetValue(key, out var id))
                return id;
            return SanitizeId(string.IsNullOrWhiteSpace(svc.ServiceId) ? svc.ServiceName : svc.ServiceId);
        }

        var ds = manifest.Datastores.FirstOrDefault(d =>
            d.DatastoreId.Equals(sourceOrTargetId, StringComparison.OrdinalIgnoreCase));

        if (ds is null) return null;

        {
            var key = $"ds:{ds.DatastoreId}";
            if (!string.IsNullOrWhiteSpace(ds.DatastoreId) && nodeIds.TryGetValue(key, out var id))
                return id;
            return SanitizeId(string.IsNullOrWhiteSpace(ds.DatastoreId) ? ds.DatastoreName : ds.DatastoreId);
        }
    }

    private static string BuildServiceLabel(ManifestService service, bool includeRuntimePlatform)
    {
        if (!includeRuntimePlatform)
            return service.ServiceName;
        return string.IsNullOrWhiteSpace(service.RuntimePlatform.ToString())
            ? service.ServiceName
            : $"{service.ServiceName}\\n{service.RuntimePlatform}";
    }

    private static string BuildDatastoreLabel(ManifestDatastore datastore, bool includeRuntimePlatform)
    {
        if (!includeRuntimePlatform)
            return datastore.DatastoreName;
        return string.IsNullOrWhiteSpace(datastore.RuntimePlatform.ToString())
            ? datastore.DatastoreName
            : $"{datastore.DatastoreName}\\n{datastore.RuntimePlatform}";
    }

    private static string EnsureUnique(string baseId, HashSet<string> used)
    {
        if (used.Add(baseId))
            return baseId;

        for (var i = 2; i < 10_000; i++)
        {
            var candidate = $"{baseId}_{i}";
            if (used.Add(candidate))
                return candidate;
        }

        // Extremely unlikely; fall back to a GUID suffix.
        var guidCandidate = $"{baseId}_{Guid.NewGuid():N}";
        used.Add(guidCandidate);
        return guidCandidate;
    }

    private static string SanitizeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "node_unknown";

        var chars = value.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
        var cleaned = new string(chars);
        if (string.IsNullOrWhiteSpace(cleaned))
            cleaned = "node_unknown";
        if (char.IsDigit(cleaned[0]))
            cleaned = $"n_{cleaned}";
        return cleaned;
    }

    private static string EscapeLabel(string value) => (value).Replace("\"", "\\\"");

    private static string NormalizeLayout(string? value)
    {
        var v = (value ?? "LR").Trim().ToUpperInvariant();
        return v switch
        {
            "TB" => "TB",
            _ => "LR"
        };
    }

    private static string NormalizeRelationshipLabels(string? value)
    {
        var v = (value ?? "type").Trim().ToLowerInvariant();
        return v switch
        {
            "none" => "none",
            _ => "type"
        };
    }

    private static string NormalizeGroupBy(string? value)
    {
        var v = (value ?? "none").Trim().ToLowerInvariant();
        return v switch
        {
            "runtimeplatform" => "runtimeplatform",
            "servicetype" => "servicetype",
            _ => "none"
        };
    }
}

