using ArchLucid.Core.Secrets;

namespace ArchLucid.Host.Core.Configuration.Secrets;

/// <summary>Reads secrets from configuration keys / environment (local dev default).</summary>
public sealed class EnvironmentVariableSecretProvider(IConfiguration configuration) : ISecretProvider
{
    private readonly IConfiguration _configuration =
        configuration ?? throw new ArgumentNullException(nameof(configuration));

    public Task<string?> GetSecretAsync(string secretName, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        string? value = _configuration[secretName];

        return Task.FromResult(string.IsNullOrWhiteSpace(value) ? null : value);
    }
}
