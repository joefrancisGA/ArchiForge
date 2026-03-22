using System.Net;
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
}
