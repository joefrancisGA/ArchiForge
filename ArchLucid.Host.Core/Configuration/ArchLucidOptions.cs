namespace ArchLucid.Host.Core.Configuration;

public sealed class ArchLucidOptions
{
    public const string SectionName = "ArchLucid";

    /// <summary>
    /// InMemory (tests/local) or Sql (durable). Null/empty when no <c>ArchLucid</c> storage value is configured
    /// (legacy product sections are not read). At runtime, unset is treated as Sql — see <see cref="EffectiveIsSql"/>.
    /// </summary>
    public string? StorageProvider
    {
        get;
        set;
    }

    /// <summary>True when <paramref name="storageProvider"/> is Sql or unset (null/whitespace).</summary>
    public static bool EffectiveIsSql(string? storageProvider) =>
        string.IsNullOrWhiteSpace(storageProvider)
        || string.Equals(storageProvider, "Sql", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when <paramref name="storageProvider"/> is explicitly InMemory.</summary>
    public static bool EffectiveIsInMemory(string? storageProvider) =>
        string.Equals(storageProvider, "InMemory", StringComparison.OrdinalIgnoreCase);
}
