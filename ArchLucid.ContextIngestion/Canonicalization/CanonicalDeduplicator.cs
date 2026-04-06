using ArchiForge.ContextIngestion.Models;

namespace ArchiForge.ContextIngestion.Canonicalization;

public class CanonicalDeduplicator : ICanonicalDeduplicator
{
    public IReadOnlyList<CanonicalObject> Deduplicate(
        IEnumerable<CanonicalObject> items)
    {
        return items
            .GroupBy(
                x => $"{x.ObjectType}|{x.Name}|{GetDedupeFingerprint(x)}",
                StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    /// <summary>
    /// Stable identity for deduplication. Precedence: <c>text</c> → <c>reference</c> → empty.
    /// Aligns with connectors that emit policy refs without a <c>text</c> property.
    /// </summary>
    internal static string GetDedupeFingerprint(CanonicalObject item)
    {
        if (item.Properties.TryGetValue("text", out string? text) && !string.IsNullOrEmpty(text))
            return text;

        if (item.Properties.TryGetValue("reference", out string? reference) && !string.IsNullOrEmpty(reference))
            return reference;

        if (item.Properties.TryGetValue("terraformType", out string? terraformType) && !string.IsNullOrEmpty(terraformType))
            return terraformType;

        if (item.Properties.TryGetValue("resourceType", out string? resourceType) && !string.IsNullOrEmpty(resourceType))
            return resourceType;

        return string.Empty;
    }
}
