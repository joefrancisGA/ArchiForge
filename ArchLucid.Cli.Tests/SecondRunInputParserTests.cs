using System.Text;

using ArchLucid.Cli.SecondRun;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class SecondRunInputParserTests
{
    private const string ValidToml =
        """
        name = "Demo.System"
        description = "At least ten characters here for validation."
        components = ["api"]
        data_stores = ["postgres"]
        """;

    [Fact]
    public void ParseFromUtf8_toml_positive_maps_architecture_request()
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(ValidToml);
        SecondRunParseOutcome outcome = SecondRunInputParser.ParseFromUtf8(utf8, "inline");

        outcome.IsSuccess.Should().BeTrue();
        outcome.Request.Should().NotBeNull();
        outcome.Request!.SystemName.Should().Be("Demo.System");
        outcome.Request.Description.Should().Contain("ten characters");
        outcome.Request.RequiredCapabilities.Should().ContainSingle("api");
        outcome.Request.InlineRequirements.Should().Contain("Datastore: postgres");
    }

    [Fact]
    public void ParseFromUtf8_json_positive_case_insensitive_keys()
    {
        const string json = """
            {"name":"Svc","description":"1234567890ab","components":["worker"]}
            """;
        SecondRunParseOutcome outcome = SecondRunInputParser.ParseFromUtf8(Encoding.UTF8.GetBytes(json), "json");

        outcome.IsSuccess.Should().BeTrue();
        outcome.Request!.SystemName.Should().Be("Svc");
        outcome.Request.RequiredCapabilities.Should().ContainSingle("worker");
    }

    [Fact]
    public void ParseFromUtf8_missing_required_name_returns_400()
    {
        const string toml = """
            description = "1234567890ab"
            """;
        SecondRunParseOutcome outcome = SecondRunInputParser.ParseFromUtf8(Encoding.UTF8.GetBytes(toml), "x");

        outcome.IsSuccess.Should().BeFalse();
        outcome.FailureCode.Should().Be(SecondRunParseFailureCode.BadRequest);
        outcome.Message.Should().Contain("name");
    }

    [Fact]
    public void ParseFromUtf8_payload_too_large_returns_413()
    {
        byte[] huge = new byte[SecondRunInputParser.MaxUtf8Bytes + 1];
        SecondRunParseOutcome outcome = SecondRunInputParser.ParseFromUtf8(huge, "big");

        outcome.IsSuccess.Should().BeFalse();
        outcome.FailureCode.Should().Be(SecondRunParseFailureCode.PayloadTooLarge);
    }

    [Fact]
    public void ParseFromUtf8_malformed_json_returns_400()
    {
        const string json = "{ not json";
        SecondRunParseOutcome outcome = SecondRunInputParser.ParseFromUtf8(Encoding.UTF8.GetBytes(json), "bad.json");

        outcome.IsSuccess.Should().BeFalse();
        outcome.FailureCode.Should().Be(SecondRunParseFailureCode.BadRequest);
        outcome.Message.Should().Contain("Malformed JSON");
    }

    [Fact]
    public void ParseFromUtf8_malformed_toml_returns_400()
    {
        const string toml = "name = ";
        SecondRunParseOutcome outcome = SecondRunInputParser.ParseFromUtf8(Encoding.UTF8.GetBytes(toml), "bad.toml");

        outcome.IsSuccess.Should().BeFalse();
        outcome.FailureCode.Should().Be(SecondRunParseFailureCode.BadRequest);
        outcome.Message.Should().Contain("Malformed TOML");
    }
}
