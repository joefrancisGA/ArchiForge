using System.Net;
using System.Text.Json;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Policy Pack Request Validation.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class PolicyPackRequestValidationTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreatePolicyPack_InvalidInitialContentJson_Returns400WithValidationErrors()
    {
        var body = new
        {
            name = "Bad JSON pack",
            description = "",
            packType = "ProjectCustom",
            initialContentJson = "{ not valid json"
        };

        HttpResponseMessage response = await Client.PostAsync("/v1/policy-packs", JsonContent(body));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        string text = await response.Content.ReadAsStringAsync();
        text.Should().ContainEquivalentOf("InitialContentJson");
    }

    [Fact]
    public async Task PublishPolicyPack_InvalidSemVerVersion_Returns400()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/policy-packs",
            JsonContent(
                new
                {
                    name = "SemVer validation pack",
                    description = "",
                    packType = "ProjectCustom",
                    initialContentJson = "{}"
                }));
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using JsonDocument created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        Guid packId = created.RootElement.GetProperty("policyPackId").GetGuid();

        HttpResponseMessage publishResponse = await Client.PostAsync(
            $"/v1/policy-packs/{packId}/publish",
            JsonContent(new { version = "1.0", contentJson = "{}" }));

        publishResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        string text = await publishResponse.Content.ReadAsStringAsync();
        text.Should().ContainEquivalentOf("Version");
    }

    [Fact]
    public async Task AssignPolicyPack_InvalidScopeLevel_Returns400()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/policy-packs",
            JsonContent(
                new
                {
                    name = "Scope validation pack",
                    description = "",
                    packType = "ProjectCustom",
                    initialContentJson = "{}"
                }));
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using JsonDocument created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        Guid packId = created.RootElement.GetProperty("policyPackId").GetGuid();

        HttpResponseMessage assignResponse = await Client.PostAsync(
            $"/v1/policy-packs/{packId}/assign",
            JsonContent(new { version = "1.0.0", scopeLevel = "Planet" }));

        assignResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        string text = await assignResponse.Content.ReadAsStringAsync();
        text.Should().ContainEquivalentOf("ScopeLevel");
    }
}
