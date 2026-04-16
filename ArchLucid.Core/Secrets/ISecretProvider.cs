namespace ArchLucid.Core.Secrets;

/// <summary>Resolves named secrets for optional features (Key Vault or environment).</summary>
public interface ISecretProvider
{
    Task<string?> GetSecretAsync(string secretName, CancellationToken ct);
}
