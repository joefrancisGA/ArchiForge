using ArchLucid.Contracts.Governance.Preview;
using ArchLucid.Contracts.Manifest;

namespace ArchLucid.Application.Governance.Preview;

/// <summary>
///     Compares governance-relevant fields from <see cref="ManifestGovernance" /> (or objects that expose it).
///     Unchanged keys are omitted from the result for a compact preview.
/// </summary>
public static class GovernanceManifestComparer
{
    /// <summary>
    ///     Compares governance snapshots. Accepts <see cref="ManifestGovernance" />,
    ///     <see cref="GoldenManifest" /> (uses <c>.Governance</c>), or <see langword="null" />.
    /// </summary>
    /// <exception cref="ArgumentException">
    ///     Thrown when either argument is a type other than <see cref="ManifestGovernance" />,
    ///     <see cref="GoldenManifest" />, or <see langword="null" />.
    /// </exception>
    public static List<GovernanceDiffItem> Compare(object? currentGovernance, object? previewGovernance)
    {
        Dictionary<string, string?> current = ExtractGovernanceFields(ToManifestGovernance(currentGovernance));
        Dictionary<string, string?> preview = ExtractGovernanceFields(ToManifestGovernance(previewGovernance));
        return CompareDictionaries(current, preview);
    }

    private static ManifestGovernance? ToManifestGovernance(object? o)
    {
        return o switch
        {
            null => null,
            ManifestGovernance mg => mg,
            GoldenManifest gm => gm.Governance,
            _ => throw new ArgumentException(
                $"Unsupported governance type '{o.GetType().FullName}'. " +
                $"Pass a {nameof(ManifestGovernance)}, a {nameof(GoldenManifest)}, or null.",
                nameof(o))
        };
    }

    /// <summary>
    ///     Maps <see cref="ManifestGovernance" /> properties to stable string keys for diffing.
    /// </summary>
    private static Dictionary<string, string?> ExtractGovernanceFields(ManifestGovernance? g)
    {
        if (g is null)
            return [];

        return new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["ComplianceTags"] = NormalizeList(g.ComplianceTags),
            ["PolicyConstraints"] = NormalizeList(g.PolicyConstraints),
            ["RequiredControls"] = NormalizeList(g.RequiredControls),
            ["RiskClassification"] = NullIfWhiteSpace(g.RiskClassification),
            ["CostClassification"] = NullIfWhiteSpace(g.CostClassification)
        };
    }

    private static string? NullIfWhiteSpace(string? s)
    {
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }

    private static string? NormalizeList(IReadOnlyList<string>? list)
    {
        if (list is null || list.Count == 0)
            return null;
        List<string> ordered = list
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return ordered.Count == 0 ? null : string.Join(", ", ordered);
    }

    private static List<GovernanceDiffItem> CompareDictionaries(
        IReadOnlyDictionary<string, string?> current,
        IReadOnlyDictionary<string, string?> preview)
    {
        List<string> keys = current.Keys.Union(preview.Keys, StringComparer.Ordinal).ToList();
        keys.Sort(StringComparer.Ordinal);
        List<GovernanceDiffItem> items = [];

        foreach (string key in keys)
        {
            current.TryGetValue(key, out string? cur);
            preview.TryGetValue(key, out string? prev);
            string curN = cur ?? string.Empty;
            string prevN = prev ?? string.Empty;

            if (curN == prevN)
                continue;

            string changeType;
            if (string.IsNullOrEmpty(curN) && !string.IsNullOrEmpty(prevN))
                changeType = GovernanceDiffChangeType.Added;
            else if (!string.IsNullOrEmpty(curN) && string.IsNullOrEmpty(prevN))
                changeType = GovernanceDiffChangeType.Removed;
            else
                changeType = GovernanceDiffChangeType.Changed;

            items.Add(new GovernanceDiffItem
            {
                Key = key,
                ChangeType = changeType,
                CurrentValue = string.IsNullOrEmpty(curN) ? null : cur,
                PreviewValue = string.IsNullOrEmpty(prevN) ? null : prev
            });
        }

        return items;
    }
}
