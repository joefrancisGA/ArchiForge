using ArchLucid.Core.Secrets;

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Core.Configuration.Secrets;

/// <summary>Resolves secrets from Azure Key Vault with short in-memory caching.</summary>
public sealed class KeyVaultSecretProvider : ISecretProvider
{
    private readonly SecretClient _client;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _ttl;

    public KeyVaultSecretProvider(IOptions<ArchLucidSecretOptions> options, IMemoryCache cache)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(cache);

        ArchLucidSecretOptions o = options.Value;
        string? uri = o.KeyVaultUri?.Trim();

        if (string.IsNullOrWhiteSpace(uri))
            throw new InvalidOperationException("ArchLucid:Secrets:KeyVaultUri is required when Provider is KeyVault.");

        _client = new SecretClient(new Uri(uri, UriKind.Absolute), new DefaultAzureCredential());
        _cache = cache;
        _ttl = TimeSpan.FromSeconds(Math.Clamp(o.KeyVaultCacheSeconds, 30, 3600));
    }

    public async Task<string?> GetSecretAsync(string secretName, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        string cacheKey = "kv:" + secretName;

        if (_cache.TryGetValue(cacheKey, out string? cached))
            return cached;

        Azure.Response<KeyVaultSecret> response = await _client.GetSecretAsync(secretName, cancellationToken: ct);
        string? value = response.Value?.Value;

        if (!string.IsNullOrEmpty(value))
            _cache.Set(cacheKey, value, _ttl);

        return value;
    }
}
