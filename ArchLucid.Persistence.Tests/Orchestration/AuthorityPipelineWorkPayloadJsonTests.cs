using ArchLucid.ContextIngestion.Models;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Orchestration;

public sealed class AuthorityPipelineWorkPayloadJsonTests
{
    [Fact]
    public void Serialize_throws_when_payload_null()
    {
        Action act = () => AuthorityPipelineWorkPayloadJson.Serialize(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Deserialize_returns_null_for_null_json()
    {
        AuthorityPipelineWorkPayload? result = AuthorityPipelineWorkPayloadJson.Deserialize(null!);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Deserialize_returns_null_for_blank_json(string json)
    {
        AuthorityPipelineWorkPayload? result = AuthorityPipelineWorkPayloadJson.Deserialize(json);

        result.Should().BeNull();
    }

    [Fact]
    public void Serialize_round_trips_minimal_payload()
    {
        AuthorityPipelineWorkPayload payload = new()
        {
            ContextIngestionRequest = new ContextIngestionRequest
            {
                RunId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ProjectId = "default",
            },
            EvidenceBundleId = "bundle-1",
        };

        string json = AuthorityPipelineWorkPayloadJson.Serialize(payload);
        AuthorityPipelineWorkPayload? back = AuthorityPipelineWorkPayloadJson.Deserialize(json);

        back.Should().NotBeNull();
        back!.EvidenceBundleId.Should().Be("bundle-1");
        back.ContextIngestionRequest.ProjectId.Should().Be("default");
        back.ContextIngestionRequest.RunId.Should().Be(payload.ContextIngestionRequest.RunId);
    }
}
