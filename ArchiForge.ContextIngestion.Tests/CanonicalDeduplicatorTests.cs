using ArchiForge.ContextIngestion.Canonicalization;
using ArchiForge.ContextIngestion.Models;

using FluentAssertions;

namespace ArchiForge.ContextIngestion.Tests;

public sealed class CanonicalDeduplicatorTests
{
    private readonly CanonicalDeduplicator _sut = new();

    [Fact]
    public void Deduplicate_UsesReferenceFingerprint_WhenTextMissing()
    {
        List<CanonicalObject> items = new List<CanonicalObject>
        {
            new()
            {
                ObjectType = "PolicyControl",
                Name = "same-label",
                SourceType = "PolicyReference",
                SourceId = "ref-a",
                Properties = new Dictionary<string, string>
                {
                    ["reference"] = "ORG-POL-001",
                    ["status"] = "referenced"
                }
            },
            new()
            {
                ObjectType = "PolicyControl",
                Name = "same-label",
                SourceType = "PolicyReference",
                SourceId = "ref-b",
                Properties = new Dictionary<string, string>
                {
                    ["reference"] = "ORG-POL-001",
                    ["status"] = "referenced"
                }
            }
        };

        IReadOnlyList<CanonicalObject> result = _sut.Deduplicate(items);

        result.Should().HaveCount(1);
    }

    [Fact]
    public void Deduplicate_PrefersTextOverReference_ForFingerprint()
    {
        List<CanonicalObject> items = new List<CanonicalObject>
        {
            new()
            {
                ObjectType = "PolicyControl",
                Name = "x",
                SourceType = "A",
                SourceId = "a",
                Properties = new Dictionary<string, string>
                {
                    ["text"] = "alpha",
                    ["reference"] = "R1"
                }
            },
            new()
            {
                ObjectType = "PolicyControl",
                Name = "x",
                SourceType = "B",
                SourceId = "b",
                Properties = new Dictionary<string, string>
                {
                    ["text"] = "beta",
                    ["reference"] = "R1"
                }
            }
        };

        IReadOnlyList<CanonicalObject> result = _sut.Deduplicate(items);

        result.Should().HaveCount(2);
    }
}
