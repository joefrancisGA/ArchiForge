using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public sealed class PolicyPackRequestValidationTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreatePolicyPack_InvalidInitialContentJson_Returns400WithValidationErrors()
    {
        var body = new
        {
            name = "Bad JSON pack",
            description = "",
            packType = "ProjectCustom",
            initialContentJson = "{ not valid json",
        };

        var response = await Client.PostAsync("/v1/policy-packs", JsonContent(body));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var text = await response.Content.ReadAsStringAsync();
        text.Should().ContainEquivalentOf("InitialContentJson");
    }

    [Fact]
    public async Task PublishPolicyPack_InvalidSemVerVersion_Returns400()
    {
        var createResponse = await Client.PostAsync(
            "/v1/policy-packs",
            JsonContent(
                new
                {
                    name = "SemVer validation pack",
                    description = "",
                    packType = "ProjectCustom",
                    initialContentJson = "{}",
                }));
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var packId = created.RootElement.GetProperty("policyPackId").GetGuid();

        var publishResponse = await Client.PostAsync(
            $"/v1/policy-packs/{packId}/publish",
            JsonContent(new { version = "1.0", contentJson = "{}" }));

        publishResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var text = await publishResponse.Content.ReadAsStringAsync();
        text.Should().ContainEquivalentOf("Version");
    }
}
