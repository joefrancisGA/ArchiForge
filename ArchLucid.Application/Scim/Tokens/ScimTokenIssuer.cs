using System.Security.Cryptography;

using ArchLucid.Core.Scim;

namespace ArchLucid.Application.Scim.Tokens;

public sealed class ScimTokenIssuer(IScimTenantTokenRepository tokens) : IScimTokenIssuer
{
    private readonly IScimTenantTokenRepository _tokens =
        tokens ?? throw new ArgumentNullException(nameof(tokens));

    /// <inheritdoc />
    public async Task<ScimTokenIssueResult> IssueTokenAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        byte[] publicBytes = RandomNumberGenerator.GetBytes(24);
        byte[] secretBytes = RandomNumberGenerator.GetBytes(32);
        string publicKey = Base64UrlEncode(publicBytes);
        string secretPart = Base64UrlEncode(secretBytes);
        string plaintext = $"archlucid_scim.{publicKey}.{secretPart}";
        byte[] hash = ScimArgonSecretHasher.HashSecret(secretBytes, tenantId);
        Guid id = await _tokens.InsertAsync(tenantId, publicKey, hash, cancellationToken);

        CryptographicOperations.ZeroMemory(secretBytes);

        return new ScimTokenIssueResult { TokenId = id, PlaintextToken = plaintext, PublicLookupKey = publicKey };
    }

    private static string Base64UrlEncode(byte[] data)
    {
        string b64 = Convert.ToBase64String(data);

        return b64.TrimEnd('=').Replace("+", "-", StringComparison.Ordinal).Replace("/", "_", StringComparison.Ordinal);
    }
}
