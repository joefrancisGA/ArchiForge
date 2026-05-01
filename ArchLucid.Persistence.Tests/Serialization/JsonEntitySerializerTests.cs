using ArchLucid.Persistence.Serialization;

using System.Text.Json;

namespace ArchLucid.Persistence.Tests.Serialization;

[Trait("Category", "Unit")]
public sealed class JsonEntitySerializerTests
{
    [Fact]
    public void Serialize_round_trips_plain_payload()
    {
        List<string> list = ["a", "beta"];
        string json = JsonEntitySerializer.Serialize(list);

        JsonEntitySerializer.Deserialize<List<string>>(json).Should().BeEquivalentTo(list);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Deserialize_throws_when_json_empty_or_whitespace(string json)
    {
        Action act = () => JsonEntitySerializer.Deserialize<List<string>>(json);

        act.Should().Throw<InvalidOperationException>().WithMessage("*empty JSON*");
    }

    [Fact]
    public void Deserialize_wraps_corrupt_payload_as_invalid_operation_with_inner_json_exception()
    {
        Action act = () => JsonEntitySerializer.Deserialize<List<string>>("{ not json ");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*corrupt*")
            .WithInnerException<JsonException>();
    }

    [Fact]
    public void Deserialize_throws_when_json_null_literal_for_reference_type()
    {
        Action act = () => JsonEntitySerializer.Deserialize<List<string>>("null");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Failed to deserialize*");
    }
}
