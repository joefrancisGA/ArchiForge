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
        if (item.Properties.TryGetValue("text", out var text) && !string.IsNullOrEmpty(text))
            return text;

        if (item.Properties.TryGetValue("reference", out var reference) && !string.IsNullOrEmpty(reference))
            return reference;

        return string.Empty;
    }
}
