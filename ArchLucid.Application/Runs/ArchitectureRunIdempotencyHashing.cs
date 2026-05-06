using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;

namespace ArchLucid.Application.Runs;
/// <summary>
///     Normalises <c>Idempotency-Key</c> and builds a stable SHA-256 fingerprint of <see cref="ArchitectureRequest"/>
///     (same <see cref="ContractJson.Default"/> options used when persisting <c>ArchitectureRequests.RequestJson</c>).
/// </summary>
public static class ArchitectureRunIdempotencyHashing
{
    /// <summary>Reject keys longer than this after trim (defensive bound).</summary>
    public const int MaxIdempotencyKeyLength = 256;
    /// <summary>SHA-256 over UTF-8 key bytes.</summary>
    public static byte[] HashIdempotencyKey(string trimmedKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trimmedKey);
        return SHA256.HashData(Encoding.UTF8.GetBytes(trimmedKey));
    }

    /// <summary>SHA-256 over canonical contract JSON (camelCase, indented, nulls omitted).</summary>
    public static byte[] FingerprintRequest(ArchitectureRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        string json = JsonSerializer.Serialize(request, ContractJson.Default);
        return SHA256.HashData(Encoding.UTF8.GetBytes(json));
    }
}