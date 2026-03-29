using ArchiForge.ContextIngestion.Models;
using ArchiForge.Persistence.ContextSnapshots;
using ArchiForge.Persistence.Serialization;

using FluentAssertions;

namespace ArchiForge.Persistence.Tests;

/// <summary>
/// Unit tests for legacy JSON fallback parsing (no SQL Server required).
/// </summary>
public sealed class ContextSnapshotJsonFallbackTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("[]")]
    public void DeserializeCanonicalObjects_empty_inputs_yield_empty_list(string? json)
    {
        List<CanonicalObject> list = ContextSnapshotJsonFallback.DeserializeCanonicalObjects(json);
        list.Should().BeEmpty();
    }

    [Fact]
    public void DeserializeCanonicalObjects_round_trips_via_serializer()
    {
        List<CanonicalObject> original =
        [
            new()
            {
                ObjectId = "o1",
                ObjectType = "T",
                Name = "N",
                SourceType = "ST",
                SourceId = "SI",
                Properties = new Dictionary<string, string>(StringComparer.Ordinal) { ["k"] = "v" }
            }
        ];

        string json = JsonEntitySerializer.Serialize(original);
        List<CanonicalObject> parsed = ContextSnapshotJsonFallback.DeserializeCanonicalObjects(json);
        parsed.Should().HaveCount(1);
        parsed[0].ObjectId.Should().Be("o1");
        parsed[0].Properties["k"].Should().Be("v");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("[]")]
    public void DeserializeStringList_empty_inputs_yield_empty_list(string? json)
    {
        List<string> list = ContextSnapshotJsonFallback.DeserializeStringList(json);
        list.Should().BeEmpty();
    }

    [Fact]
    public void DeserializeStringList_parses_json_array()
    {
        string json = JsonEntitySerializer.Serialize(new List<string> { "a", "b" });
        ContextSnapshotJsonFallback.DeserializeStringList(json).Should().Equal("a", "b");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{}")]
    public void DeserializeStringDictionary_empty_inputs_yield_empty_dictionary(string? json)
    {
        Dictionary<string, string> dict = ContextSnapshotJsonFallback.DeserializeStringDictionary(json);
        dict.Should().BeEmpty();
    }

    [Fact]
    public void DeserializeStringDictionary_parses_json_object()
    {
        string json = JsonEntitySerializer.Serialize(
            new Dictionary<string, string>(StringComparer.Ordinal) { ["path"] = "hash" });

        Dictionary<string, string> dict = ContextSnapshotJsonFallback.DeserializeStringDictionary(json);
        dict["path"].Should().Be("hash");
    }
}
