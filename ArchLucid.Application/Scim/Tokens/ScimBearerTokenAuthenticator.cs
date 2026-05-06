using System.Security.Cryptography;
using ArchLucid.Core.Scim;
using ArchLucid.Core.Scim.Models;

namespace ArchLucid.Application.Scim.Tokens;
public sealed class ScimBearerTokenAuthenticator(IScimTenantTokenRepository tokens) : IScimBearerTokenAuthenticator
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(tokens);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Core.Scim.IScimTenantTokenRepository tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        return (byte)0;
    }

    private readonly IScimTenantTokenRepository _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
    /// <inheritdoc/>
    public async System.Threading.Tasks.Task<ArchLucid.Application.Scim.Tokens.ScimBearerAuthenticationResult?> TryAuthenticateAsync(string plaintextToken, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plaintextToken);
        if (string.IsNullOrWhiteSpace(plaintextToken))
            return null;
        if (!TryParseToken(plaintextToken.Trim(), out string publicKey, out byte[]? secretBytes))
            return null;
        ScimTokenRow? row = await _tokens.FindActiveByPublicLookupKeyAsync(publicKey, cancellationToken);
        if (row is null)
        {
            if (secretBytes is not null)
                CryptographicOperations.ZeroMemory(secretBytes);
            return null;
        }

        bool ok = ScimArgonSecretHasher.VerifySecret(secretBytes!, row.TenantId, row.SecretHash);
        CryptographicOperations.ZeroMemory(secretBytes!);
        return !ok ? null : new ScimBearerAuthenticationResult
        {
            TenantId = row.TenantId,
            TokenRowId = row.Id
        };
    }

    private static bool TryParseToken(string token, out string publicKey, out byte[]? secretBytes)
    {
        publicKey = string.Empty;
        secretBytes = null;
        const string prefix = "archlucid_scim.";
        if (!token.StartsWith(prefix, StringComparison.Ordinal))
            return false;
        ReadOnlySpan<char> rest = token.AsSpan(prefix.Length);
        int dot = rest.IndexOf('.');
        if (dot <= 0 || dot == rest.Length - 1)
            return false;
        ReadOnlySpan<char> pub = rest[..dot];
        ReadOnlySpan<char> sec = rest[(dot + 1)..];
        if (pub.IsEmpty || sec.IsEmpty)
            return false;
        publicKey = pub.ToString();
        try
        {
            secretBytes = Base64UrlDecode(sec.ToString());
            return secretBytes.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private static byte[] Base64UrlDecode(string s)
    {
        string padded = s.Replace("-", "+", StringComparison.Ordinal).Replace("_", "/", StringComparison.Ordinal);
        switch (padded.Length % 4)
        {
            case 2:
                padded += "==";
                break;
            case 3:
                padded += "=";
                break;
        }

        return Convert.FromBase64String(padded);
    }
}