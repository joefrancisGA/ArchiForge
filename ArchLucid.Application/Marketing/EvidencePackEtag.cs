using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace ArchLucid.Application.Marketing;

/// <summary>
///     Computes a content-driven SHA-256 ETag for the Trust Center evidence pack.
/// </summary>
/// <remarks>
///     <para>
///         The hash input is, for each entry in the supplied order:
///         <c>BE int32(name UTF-8 length) | name UTF-8 bytes | BE int32(content length) | content bytes</c>.
///         A single <c>0xFF</c> separator byte is written between entries.
///     </para>
///     <para>
///         The encoding is intentionally length-prefixed so that two adjacent files cannot
///         be reorganised into a different (name, content) split that hashes to the same value.
///         The output is wrapped in double quotes per RFC 9110 strong-ETag syntax so the
///         controller can pass it straight into the <c>ETag</c> response header.
///     </para>
/// </remarks>
public static class EvidencePackEtag
{
    /// <summary>Computes the strong ETag value (already quoted) for the given ordered entries.</summary>
    public static string Compute(IReadOnlyList<EvidencePackEntry> entries)
    {
        if (entries is null) throw new ArgumentNullException(nameof(entries));

        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> lengthBuffer = stackalloc byte[4];

        for (int i = 0; i < entries.Count; i++)
        {
            EvidencePackEntry entry = entries[i];

            if (entry is null) throw new ArgumentException("Entry must not be null.", nameof(entries));
            if (entry.ZipName is null) throw new ArgumentException("Entry name must not be null.", nameof(entries));
            if (entry.Content is null) throw new ArgumentException("Entry content must not be null.", nameof(entries));

            byte[] nameBytes = Encoding.UTF8.GetBytes(entry.ZipName);

            BinaryPrimitives.WriteInt32BigEndian(lengthBuffer, nameBytes.Length);
            hash.AppendData(lengthBuffer);
            hash.AppendData(nameBytes);

            BinaryPrimitives.WriteInt32BigEndian(lengthBuffer, entry.Content.Length);
            hash.AppendData(lengthBuffer);
            hash.AppendData(entry.Content);

            hash.AppendData([0xFF]);
        }

        byte[] digest = hash.GetHashAndReset();
        return $"\"{Convert.ToHexString(digest).ToLowerInvariant()}\"";
    }
}
