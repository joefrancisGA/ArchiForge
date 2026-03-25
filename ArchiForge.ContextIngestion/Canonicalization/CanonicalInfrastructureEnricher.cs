using ArchiForge.ContextIngestion.Models;

namespace ArchiForge.ContextIngestion.Canonicalization;

public class CanonicalInfrastructureEnricher : ICanonicalEnricher
{
    public IReadOnlyList<CanonicalObject> Enrich(IEnumerable<CanonicalObject> items)
    {
        List<CanonicalObject> results = new();

        foreach (CanonicalObject item in items)
        {
            if (string.Equals(item.ObjectType, "TopologyResource", StringComparison.OrdinalIgnoreCase))
            {
                if (!item.Properties.ContainsKey("category"))
                    item.Properties["category"] = InferCategory(item);
            }

            if (string.Equals(item.ObjectType, "SecurityBaseline", StringComparison.OrdinalIgnoreCase))
            {
                if (!item.Properties.ContainsKey("status"))
                    item.Properties["status"] = "declared";
            }

            results.Add(item);
        }

        return results;
    }

    private static string InferCategory(CanonicalObject item)
    {
        if (item.Properties.TryGetValue("terraformType", out string? terraformType))
        {
            string t = terraformType.ToLowerInvariant();

            if (t.Contains("virtual_network", StringComparison.OrdinalIgnoreCase) ||
                t.Contains("subnet", StringComparison.OrdinalIgnoreCase))
                return "network";

            if (t.Contains("storage", StringComparison.OrdinalIgnoreCase))
                return "storage";

            if (t.Contains("web_app", StringComparison.OrdinalIgnoreCase) ||
                t.Contains("linux_web_app", StringComparison.OrdinalIgnoreCase) ||
                t.Contains("container", StringComparison.OrdinalIgnoreCase))
                return "compute";

            if (t.Contains("sql", StringComparison.OrdinalIgnoreCase) ||
                t.Contains("postgres", StringComparison.OrdinalIgnoreCase) ||
                t.Contains("database", StringComparison.OrdinalIgnoreCase))
                return "data";
        }

        if (item.Properties.TryGetValue("resourceType", out string? resourceType))
        {
            string r = resourceType.ToLowerInvariant();

            if (r.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                r.Contains("subnet", StringComparison.OrdinalIgnoreCase) ||
                r.Contains("vnet", StringComparison.OrdinalIgnoreCase))
                return "network";

            if (r.Contains("storage", StringComparison.OrdinalIgnoreCase))
                return "storage";

            if (r.Contains("compute", StringComparison.OrdinalIgnoreCase) ||
                r.Contains("appservice", StringComparison.OrdinalIgnoreCase) ||
                r.Contains("container", StringComparison.OrdinalIgnoreCase))
                return "compute";

            if (r.Contains("database", StringComparison.OrdinalIgnoreCase))
                return "data";

            if (r.Contains("identity", StringComparison.OrdinalIgnoreCase))
                return "identity";
        }

        return "general";
    }
}
