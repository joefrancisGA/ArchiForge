using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using ArchLucid.Contracts.Common;

namespace ArchLucid.Contracts.Manifest;

/// <summary>
///     Deterministic SHA-256 fingerprint for a <see cref="GoldenManifest" /> serialized with
///     <see cref="ContractJson.Default" />.
/// </summary>
public static class GoldenManifestFingerprint
{
    /// <summary>Uppercase hex SHA-256 over canonical contract JSON for <paramref name="manifest" />.</summary>
    public static string ComputeSha256Hex(GoldenManifest manifest)
    {
        if (manifest is null)
            throw new ArgumentNullException(nameof(manifest));

        byte[] utf8 = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(manifest, ContractJson.Default));

        return Convert.ToHexString(SHA256.HashData(utf8));
    }

    /// <summary>Parses JSON then computes <see cref="ComputeSha256Hex" /> (stable round-trip).</summary>
    public static string ComputeSha256HexFromManifestJson(string manifestJson)
    {
        if (string.IsNullOrWhiteSpace(manifestJson))
            throw new ArgumentException("Manifest JSON is required.", nameof(manifestJson));

        GoldenManifest? manifest = JsonSerializer.Deserialize<GoldenManifest>(manifestJson, ContractJson.Default);

        return manifest is null ? throw new JsonException("Manifest JSON deserialized to null.") : ComputeSha256Hex(manifest);
    }
}
