namespace ArchLucid.Core.Secrets;

/// <summary>Configuration for <see cref="ISecretProvider"/>.</summary>
public sealed class ArchLucidSecretOptions
{
    public const string SectionName = "ArchLucid:Secrets";

    public SecretProviderKind Provider { get; set; } = SecretProviderKind.EnvironmentVariable;

    /// <summary>Azure Key Vault URI, e.g. <c>https://{vault}.vault.azure.net/</c>.</summary>
    public string? KeyVaultUri { get; set; }

    /// <summary>Optional cache TTL for Key Vault secret reads.</summary>
    public int KeyVaultCacheSeconds { get; set; } = 300;
}
