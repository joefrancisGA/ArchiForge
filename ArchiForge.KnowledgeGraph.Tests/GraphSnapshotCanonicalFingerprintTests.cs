using ArchiForge.ContextIngestion.Models;
using ArchiForge.KnowledgeGraph.Services;

using FluentAssertions;

namespace ArchiForge.KnowledgeGraph.Tests;

[Trait("Category", "Unit")]
public sealed class GraphSnapshotCanonicalFingerprintTests
{
    [Fact]
    public void AreEquivalent_WhenPreviousNull_ReturnsFalse()
    {
        ContextSnapshot current = BuildSnapshot("p1", [new CanonicalObject { ObjectId = "a", ObjectType = "t", Name = "n", SourceType = "s", SourceId = "1" }]);

        bool equivalent = GraphSnapshotCanonicalFingerprint.AreEquivalent(null, current);

        equivalent.Should().BeFalse();
    }

    [Fact]
    public void AreEquivalent_WhenSameSnapshotId_ReturnsFalse()
    {
        Guid id = Guid.NewGuid();
        ContextSnapshot a = BuildSnapshot("p1", [], id);
        ContextSnapshot b = BuildSnapshot("p1", [], id);

        bool equivalent = GraphSnapshotCanonicalFingerprint.AreEquivalent(a, b);

        equivalent.Should().BeFalse();
    }

    [Fact]
    public void AreEquivalent_WhenCanonicalSetsMatch_ReturnsTrue()
    {
        List<CanonicalObject> objects =
        [
            new() { ObjectId = "b", ObjectType = "type", Name = "B", SourceType = "src", SourceId = "2" },
            new() { ObjectId = "a", ObjectType = "type", Name = "A", SourceType = "src", SourceId = "1" }
        ];

        ContextSnapshot previous = BuildSnapshot("proj", objects, Guid.NewGuid());
        ContextSnapshot current = BuildSnapshot("proj", objects, Guid.NewGuid());

        bool equivalent = GraphSnapshotCanonicalFingerprint.AreEquivalent(previous, current);

        equivalent.Should().BeTrue();
    }

    [Fact]
    public void Compute_IsOrderInsensitiveForCanonicalObjects()
    {
        List<CanonicalObject> setA =
        [
            new() { ObjectId = "a", ObjectType = "t", Name = "n", SourceType = "s", SourceId = "1" },
            new() { ObjectId = "b", ObjectType = "t", Name = "n2", SourceType = "s", SourceId = "2" }
        ];
        List<CanonicalObject> setB =
        [
            new() { ObjectId = "b", ObjectType = "t", Name = "n2", SourceType = "s", SourceId = "2" },
            new() { ObjectId = "a", ObjectType = "t", Name = "n", SourceType = "s", SourceId = "1" }
        ];

        string fa = GraphSnapshotCanonicalFingerprint.Compute(BuildSnapshot("p", setA));
        string fb = GraphSnapshotCanonicalFingerprint.Compute(BuildSnapshot("p", setB));

        fa.Should().Be(fb);
    }

    private static ContextSnapshot BuildSnapshot(string projectId, List<CanonicalObject> objects, Guid? snapshotId = null)
    {
        return new ContextSnapshot
        {
            SnapshotId = snapshotId ?? Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            ProjectId = projectId,
            CreatedUtc = DateTime.UtcNow,
            CanonicalObjects = objects
        };
    }
}
