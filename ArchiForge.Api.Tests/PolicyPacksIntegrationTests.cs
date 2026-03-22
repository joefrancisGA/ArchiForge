using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public sealed class PolicyPacksIntegrationTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task PolicyPack_CreateAssignEffective_Lifecycle()
    {
        var contentJson = """
            {
              "complianceRuleIds": [],
              "complianceRuleKeys": [],
              "alertRuleIds": [],
              "compositeAlertRuleIds": [],
              "advisoryDefaults": { "scanDepth": "standard" },
              "metadata": { "tier": "test" }
            }
            """;

        var createResponse = await Client.PostAsync(
            "/v1/policy-packs",
            JsonContent(
                new
                {
                    name = "Integration test pack",
                    description = "lifecycle",
                    packType = "ProjectCustom",
                    initialContentJson = contentJson,
                }));

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);
        created.Should().NotBeNull();
        var packId = created!.PolicyPackId;

        var assignResponse = await Client.PostAsync(
            $"/v1/policy-packs/{packId}/assign",
            JsonContent(new { version = "1.0.0" }));

        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var effectiveResponse = await Client.GetAsync("/v1/policy-packs/effective");
        effectiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var effective = await effectiveResponse.Content.ReadFromJsonAsync<EffectivePolicyPackSetResponse>(JsonOptions);
        effective.Should().NotBeNull();
        effective!.Packs.Should().HaveCount(1);
        effective.Packs[0].PolicyPackId.Should().Be(packId);
        effective.Packs[0].Version.Should().Be("1.0.0");

        var mergedResponse = await Client.GetAsync("/v1/policy-packs/effective-content");
        mergedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var merged = await mergedResponse.Content.ReadFromJsonAsync<PolicyPackContentResponse>(JsonOptions);
        merged.Should().NotBeNull();
        merged!.AdvisoryDefaults.Should().ContainKey("scanDepth");
        merged.AdvisoryDefaults["scanDepth"].Should().Be("standard");
        merged.Metadata.Should().ContainKey("tier");
    }

    [Fact]
    public async Task PublishSameVersionTwice_IsIdempotent_SingleVersionRow()
    {
        var createResponse = await Client.PostAsync(
            "/v1/policy-packs",
            JsonContent(
                new
                {
                    name = "Idempotent publish pack",
                    description = "",
                    packType = "ProjectCustom",
                    initialContentJson = "{}",
                }));
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);
        var packId = created!.PolicyPackId;

        var bodyA = new { version = "1.0.0", contentJson = """{"metadata":{"k":"a"}}""" };
        var bodyB = new { version = "1.0.0", contentJson = """{"metadata":{"k":"b"}}""" };

        var p1 = await Client.PostAsync($"/v1/policy-packs/{packId}/publish", JsonContent(bodyA));
        p1.StatusCode.Should().Be(HttpStatusCode.OK);
        var v1 = await p1.Content.ReadFromJsonAsync<PolicyPackVersionResponse>(JsonOptions);
        v1.Should().NotBeNull();

        var p2 = await Client.PostAsync($"/v1/policy-packs/{packId}/publish", JsonContent(bodyB));
        p2.StatusCode.Should().Be(HttpStatusCode.OK);
        var v2 = await p2.Content.ReadFromJsonAsync<PolicyPackVersionResponse>(JsonOptions);
        v2!.PolicyPackVersionId.Should().Be(v1!.PolicyPackVersionId);

        var list = await Client.GetAsync($"/v1/policy-packs/{packId}/versions");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var versions = await list.Content.ReadFromJsonAsync<List<PolicyPackVersionResponse>>(JsonOptions);
        versions.Should().NotBeNull();
        versions!.Count(x => x.Version == "1.0.0").Should().Be(1);
        versions.Single(x => x.Version == "1.0.0").ContentJson.Should().Contain("\"k\":\"b\"");
    }

    [Fact]
    public async Task ListVersions_AfterCreate_IncludesInitialVersion()
    {
        var createResponse = await Client.PostAsync(
            "/v1/policy-packs",
            JsonContent(
                new
                {
                    name = "Versions list pack",
                    description = "",
                    packType = "ProjectCustom",
                    initialContentJson = "{}",
                }));
        var created = await createResponse.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);
        var packId = created!.PolicyPackId;

        var list = await Client.GetAsync($"/v1/policy-packs/{packId}/versions");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var versions = await list.Content.ReadFromJsonAsync<List<PolicyPackVersionResponse>>(JsonOptions);
        versions.Should().ContainSingle(v => v.Version == "1.0.0" && v.IsPublished == false);
    }

    [Fact]
    public async Task AssignUnknownVersion_Returns404()
    {
        var createResponse = await Client.PostAsync(
            "/v1/policy-packs",
            JsonContent(
                new
                {
                    name = "Assign 404 pack",
                    description = "",
                    packType = "ProjectCustom",
                    initialContentJson = "{}",
                }));
        var created = await createResponse.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);
        var packId = created!.PolicyPackId;

        var assign = await Client.PostAsync(
            $"/v1/policy-packs/{packId}/assign",
            JsonContent(new { version = "99.0.0" }));

        assign.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var text = await assign.Content.ReadAsStringAsync();
        text.Should().ContainEquivalentOf("policy-pack-version-not-found");
    }

    private sealed class PolicyPackResponse
    {
        public Guid PolicyPackId { get; init; }
        public string Name { get; init; } = "";
    }

    private sealed class PolicyPackVersionResponse
    {
        public Guid PolicyPackVersionId { get; init; }
        public string Version { get; init; } = "";
        public string ContentJson { get; init; } = "";
        public bool IsPublished { get; init; }
    }

    private sealed class EffectivePolicyPackSetResponse
    {
        public List<ResolvedPackResponse> Packs { get; init; } = [];
    }

    private sealed class ResolvedPackResponse
    {
        public Guid PolicyPackId { get; set; }
        public string Version { get; set; } = "";
    }

    private sealed class PolicyPackContentResponse
    {
        public Dictionary<string, string> AdvisoryDefaults { get; init; } = new();
        public Dictionary<string, string> Metadata { get; init; } = new();
    }
}
