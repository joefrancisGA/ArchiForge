using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

using ArchLucid.Application.Marketing;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Marketing;

[Trait("Category", "Unit")]
public sealed class EvidencePackEtagTests
{
    [Fact]
    public void Compute_ProducesQuotedHexSha256OfLengthPrefixedEntries()
    {
        EvidencePackEntry entry1 = new("a.md", "alpha"u8.ToArray());
        EvidencePackEntry entry2 = new("b.md", "beta"u8.ToArray());

        string actual = EvidencePackEtag.Compute([entry1, entry2]);
        string expected = ComputeReferenceEtag([entry1, entry2]);

        actual.Should().Be(expected);
        actual.Should().StartWith("\"").And.EndWith("\"");
        actual.Length.Should().Be(2 + 64);
    }

    [Fact]
    public void Compute_IsDeterministicAcrossInvocations()
    {
        EvidencePackEntry entry = new("doc.md", "hello"u8.ToArray());

        string first = EvidencePackEtag.Compute([entry]);
        string second = EvidencePackEtag.Compute([entry]);

        second.Should().Be(first);
    }

    [Fact]
    public void Compute_ChangesWhenContentBytesChange()
    {
        EvidencePackEntry original = new("doc.md", "hello"u8.ToArray());
        EvidencePackEntry modified = new("doc.md", "hello!"u8.ToArray());

        string before = EvidencePackEtag.Compute([original]);
        string after = EvidencePackEtag.Compute([modified]);

        after.Should().NotBe(before);
    }

    [Fact]
    public void Compute_ChangesWhenEntryNameChanges()
    {
        EvidencePackEntry original = new("doc.md", "x"u8.ToArray());
        EvidencePackEntry renamed = new("DOC.md", "x"u8.ToArray());

        string before = EvidencePackEtag.Compute([original]);
        string after = EvidencePackEtag.Compute([renamed]);

        after.Should().NotBe(before);
    }

    [Fact]
    public void Compute_ChangesWhenEntryOrderChanges()
    {
        EvidencePackEntry a = new("a.md", "alpha"u8.ToArray());
        EvidencePackEntry b = new("b.md", "beta"u8.ToArray());

        string ab = EvidencePackEtag.Compute([a, b]);
        string ba = EvidencePackEtag.Compute([b, a]);

        ba.Should().NotBe(ab);
    }

    [Fact]
    public void Compute_LengthPrefixingPreventsBoundaryCollision()
    {
        // Two different (name, content) splits whose concatenated bytes are identical
        // must NOT produce the same ETag — that is the whole reason for length prefixing.
        EvidencePackEntry split1 = new("ab", "cd"u8.ToArray());
        EvidencePackEntry split2 = new("a", "bcd"u8.ToArray());

        string left = EvidencePackEtag.Compute([split1]);
        string right = EvidencePackEtag.Compute([split2]);

        right.Should().NotBe(left);
    }

    [Fact]
    public void Compute_ThrowsOnNullList()
    {
        Action act = () => EvidencePackEtag.Compute(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Compute_ThrowsOnNullEntry()
    {
        EvidencePackEntry[] entries = [null!];

        Action act = () => EvidencePackEtag.Compute(entries);
        act.Should().Throw<ArgumentException>();
    }

    private static string ComputeReferenceEtag(IReadOnlyList<EvidencePackEntry> entries)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> lengthBuffer = stackalloc byte[4];

        foreach (EvidencePackEntry entry in entries)
        {
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
