using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using ArchiForge.Api.Routing;

using FluentAssertions;

using JetBrains.Annotations;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class PolicyPacksIntegrationTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task PolicyPack_CreateAssignEffective_Lifecycle()
    {
        string contentJson = """
                             {
                               "complianceRuleIds": [],
                               "complianceRuleKeys": [],
                               "alertRuleIds": [],
                               "compositeAlertRuleIds": [],
                               "advisoryDefaults": { "scanDepth": "standard" },
                               "metadata": { "tier": "test" }
                             }
                             """;

        HttpResponseMessage createResponse = await Client.PostAsync(
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
        PolicyPackResponse? created = await createResponse.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);
        created.Should().NotBeNull();
        Guid packId = created.PolicyPackId;

        HttpResponseMessage assignResponse = await Client.PostAsync(
            $"/v1/policy-packs/{packId}/assign",
            JsonContent(new
            {
                version = "1.0.0"
            }));

        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage effectiveResponse = await Client.GetAsync("/v1/policy-packs/effective");
        effectiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        EffectivePolicyPackSetResponse? effective = await effectiveResponse.Content.ReadFromJsonAsync<EffectivePolicyPackSetResponse>(JsonOptions);
        effective.Should().NotBeNull();
        effective.Packs.Should().HaveCount(1);
        effective.Packs[0].PolicyPackId.Should().Be(packId);
        ResolvedPackResponse.Version.Should().Be("1.0.0");

        HttpResponseMessage mergedResponse = await Client.GetAsync("/v1/policy-packs/effective-content");
        mergedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        PolicyPackContentResponse? merged = await mergedResponse.Content.ReadFromJsonAsync<PolicyPackContentResponse>(JsonOptions);
        merged.Should().NotBeNull();
        merged.AdvisoryDefaults.Should().ContainKey("scanDepth");
        merged.AdvisoryDefaults["scanDepth"].Should().Be("standard");
        merged.Metadata.Should().ContainKey("tier");
    }

    [Fact]
    public async Task EffectiveContent_MergesComplianceRuleKeys_FromAssignedPack()
    {
        string contentJson = """
                             {
                               "complianceRuleIds": [],
                               "complianceRuleKeys": [ "rule-alpha", "rule-beta" ],
                               "alertRuleIds": [],
                               "compositeAlertRuleIds": [],
                               "advisoryDefaults": {},
                               "metadata": {}
                             }
                             """;

        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/policy-packs",
            JsonContent(
                new
                {
                    name = "Compliance keys pack",
                    description = "",
                    packType = "ProjectCustom",
                    initialContentJson = contentJson,
                }));
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        PolicyPackResponse? created = await createResponse.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);
        Guid packId = created!.PolicyPackId;

        HttpResponseMessage assignResponse = await Client.PostAsync(
            $"/v1/policy-packs/{packId}/assign",
            JsonContent(new
            {
                version = "1.0.0"
            }));
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage mergedResponse = await Client.GetAsync("/v1/policy-packs/effective-content");
        mergedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        PolicyPackContentResponse? merged = await mergedResponse.Content.ReadFromJsonAsync<PolicyPackContentResponse>(JsonOptions);
        merged.Should().NotBeNull();
        merged.ComplianceRuleKeys.Should().BeEquivalentTo("rule-alpha", "rule-beta");
    }

    [Fact]
    public async Task EffectiveContent_MergesAlertRuleIds_FromAssignedPack()
    {
        Guid idA = Guid.NewGuid();
        Guid idB = Guid.NewGuid();
        string contentJson = $$"""
                               {
                                 "complianceRuleIds": [],
                                 "complianceRuleKeys": [],
                                 "alertRuleIds": [ "{{idA}}", "{{idB}}" ],
                                 "compositeAlertRuleIds": [],
                                 "advisoryDefaults": {},
                                 "metadata": {}
                               }
                               """;

        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/policy-packs",
            JsonContent(
                new
                {
                    name = "Alert ids pack",
                    description = "",
                    packType = "ProjectCustom",
                    initialContentJson = contentJson,
                }));
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        PolicyPackResponse? created = await createResponse.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);
        Guid packId = created!.PolicyPackId;

        HttpResponseMessage assignResponse = await Client.PostAsync(
            $"/v1/policy-packs/{packId}/assign",
            JsonContent(new
            {
                version = "1.0.0"
            }));
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage mergedResponse = await Client.GetAsync("/v1/policy-packs/effective-content");
        mergedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        PolicyPackContentResponse? merged = await mergedResponse.Content.ReadFromJsonAsync<PolicyPackContentResponse>(JsonOptions);
        merged.Should().NotBeNull();
        merged.AlertRuleIds.Should().BeEquivalentTo([idA, idB]);
    }

    [Fact]
    public async Task PublishSameVersionTwice_IsIdempotent_SingleVersionRow()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
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
        PolicyPackResponse? created = await createResponse.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);
        Guid packId = created!.PolicyPackId;

        var bodyA = new
        {
            version = "1.0.0",
            contentJson = """{"metadata":{"k":"a"}}"""
        };
        var bodyB = new
        {
            version = "1.0.0",
            contentJson = """{"metadata":{"k":"b"}}"""
        };

        HttpResponseMessage p1 = await Client.PostAsync($"/v1/policy-packs/{packId}/publish", JsonContent(bodyA));
        p1.StatusCode.Should().Be(HttpStatusCode.OK);
        PolicyPackVersionResponse? v1 = await p1.Content.ReadFromJsonAsync<PolicyPackVersionResponse>(JsonOptions);
        v1.Should().NotBeNull();

        HttpResponseMessage p2 = await Client.PostAsync($"/v1/policy-packs/{packId}/publish", JsonContent(bodyB));
        p2.StatusCode.Should().Be(HttpStatusCode.OK);
        PolicyPackVersionResponse? v2 = await p2.Content.ReadFromJsonAsync<PolicyPackVersionResponse>(JsonOptions);
        v2!.PolicyPackVersionId.Should().Be(v1.PolicyPackVersionId);

        HttpResponseMessage list = await Client.GetAsync($"/v1/policy-packs/{packId}/versions");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        List<PolicyPackVersionResponse>? versions = await list.Content.ReadFromJsonAsync<List<PolicyPackVersionResponse>>(JsonOptions);
        versions.Should().NotBeNull();
        versions.Count(x => x.Version == "1.0.0").Should().Be(1);
        versions.Single(x => x.Version == "1.0.0").ContentJson.Should().Contain("\"k\":\"b\"");
    }

    [Fact]
    public async Task ListVersions_AfterCreate_IncludesInitialVersion()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/policy-packs",
            JsonContent(
                new
                {
                    name = "Versions list pack",
                    description = "",
                    packType = "ProjectCustom",
                    initialContentJson = "{}",
                }));
        PolicyPackResponse? created = await createResponse.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);
        Guid packId = created!.PolicyPackId;

        HttpResponseMessage list = await Client.GetAsync($"/v1/policy-packs/{packId}/versions");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        List<PolicyPackVersionResponse>? versions = await list.Content.ReadFromJsonAsync<List<PolicyPackVersionResponse>>(JsonOptions);
        versions.Should().ContainSingle(v => v.Version == "1.0.0" && v.IsPublished == false);
    }

    [Fact]
    public async Task AssignUnknownVersion_Returns404()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/policy-packs",
            JsonContent(
                new
                {
                    name = "Assign 404 pack",
                    description = "",
                    packType = "ProjectCustom",
                    initialContentJson = "{}",
                }));
        PolicyPackResponse? created = await createResponse.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);
        Guid packId = created!.PolicyPackId;

        HttpResponseMessage assign = await Client.PostAsync(
            $"/v1/policy-packs/{packId}/assign",
            JsonContent(new
            {
                version = "99.0.0"
            }));

        assign.StatusCode.Should().Be(HttpStatusCode.NotFound);
        string text = await assign.Content.ReadAsStringAsync();
        using JsonDocument problem = JsonDocument.Parse(text);
        JsonElement root = problem.RootElement;
        string? typeString = root.TryGetProperty("type", out JsonElement camel)
            ? camel.GetString()
            : root.GetProperty("Type").GetString();
        typeString.Should().Contain("policy-pack-version-not-found");
    }

    [Fact]
    public async Task GovernanceResolution_Get_ReturnsNotesAndEffectiveContent()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/policy-packs",
            JsonContent(
                new
                {
                    name = "Resolution inspect pack",
                    description = "",
                    packType = "ProjectCustom",
                    initialContentJson = """
                        {
                          "complianceRuleIds": [],
                          "complianceRuleKeys": [],
                          "alertRuleIds": [],
                          "compositeAlertRuleIds": [],
                          "advisoryDefaults": {},
                          "metadata": { "k": "v" }
                        }
                        """,
                }));
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        PolicyPackResponse? created = await createResponse.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);
        Guid packId = created!.PolicyPackId;

        (await Client.PostAsync(
                $"/v1/policy-packs/{packId}/assign",
                JsonContent(new
                {
                    version = "1.0.0",
                    scopeLevel = "Project",
                    isPinned = false
                })))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage res = await Client.GetAsync($"/{ApiV1Routes.GovernanceResolution}");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        using JsonDocument doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        JsonElement root = doc.RootElement;
        root.GetProperty("notes").EnumerateArray().Should().NotBeEmpty();
        root.GetProperty("decisions").EnumerateArray().Should().NotBeEmpty();
        root.GetProperty("effectiveContent").GetProperty("metadata").GetProperty("k").GetString().Should().Be("v");
    }

    [Fact]
    public async Task EffectiveContent_MergesAdvisoryDefaults_FromTwoAssignedPacks()
    {
        string contentA = """
                          {
                            "complianceRuleIds": [],
                            "complianceRuleKeys": [],
                            "alertRuleIds": [],
                            "compositeAlertRuleIds": [],
                            "advisoryDefaults": { "scanDepth": "deep" },
                            "metadata": { "mergeTier": "baseline" }
                          }
                          """;

        string contentB = """
                          {
                            "complianceRuleIds": [],
                            "complianceRuleKeys": [],
                            "alertRuleIds": [],
                            "compositeAlertRuleIds": [],
                            "advisoryDefaults": { "notifyChannel": "email" },
                            "metadata": { "packRole": "overlay" }
                          }
                          """;

        HttpResponseMessage createA = await Client.PostAsync(
            "/v1/policy-packs",
            JsonContent(new
            {
                name = "Two-pack merge A",
                description = "",
                packType = "ProjectCustom",
                initialContentJson = contentA,
            }));
        createA.StatusCode.Should().Be(HttpStatusCode.OK);
        PolicyPackResponse? packA = await createA.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);

        HttpResponseMessage createB = await Client.PostAsync(
            "/v1/policy-packs",
            JsonContent(new
            {
                name = "Two-pack merge B",
                description = "",
                packType = "ProjectCustom",
                initialContentJson = contentB,
            }));
        createB.StatusCode.Should().Be(HttpStatusCode.OK);
        PolicyPackResponse? packB = await createB.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);

        (await Client.PostAsync(
                $"/v1/policy-packs/{packA!.PolicyPackId}/assign",
                JsonContent(new { version = "1.0.0" })))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await Client.PostAsync(
                $"/v1/policy-packs/{packB!.PolicyPackId}/assign",
                JsonContent(new { version = "1.0.0" })))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage mergedResponse = await Client.GetAsync("/v1/policy-packs/effective-content");
        mergedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        PolicyPackContentResponse? merged = await mergedResponse.Content.ReadFromJsonAsync<PolicyPackContentResponse>(JsonOptions);
        merged.Should().NotBeNull();
        merged.AdvisoryDefaults.Should().ContainKeys("scanDepth", "notifyChannel");
        merged.AdvisoryDefaults["scanDepth"].Should().Be("deep");
        merged.AdvisoryDefaults["notifyChannel"].Should().Be("email");
        merged.Metadata.Should().ContainKeys("mergeTier", "mergeSla");
        merged.Metadata["mergeTier"].Should().Be("baseline");
        merged.Metadata["mergeSla"].Should().Be("p1");
    }

    [Fact]
    public async Task EffectiveContent_UnionsComplianceRuleKeys_FromTwoAssignments()
    {
        Guid packA = await CreatePackAsync("merge-pack-a", "merge-key-a");
        Guid packB = await CreatePackAsync("merge-pack-b", "merge-key-b");

        (await Client.PostAsync(
                $"/v1/policy-packs/{packA}/assign",
                JsonContent(new
                {
                    version = "1.0.0"
                })))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await Client.PostAsync(
                $"/v1/policy-packs/{packB}/assign",
                JsonContent(new
                {
                    version = "1.0.0"
                })))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage effectiveResponse = await Client.GetAsync("/v1/policy-packs/effective");
        effectiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        EffectivePolicyPackSetResponse? effectiveSet = await effectiveResponse.Content.ReadFromJsonAsync<EffectivePolicyPackSetResponse>(JsonOptions);
        effectiveSet!.Packs.Should().HaveCount(2);

        HttpResponseMessage mergedResponse = await Client.GetAsync("/v1/policy-packs/effective-content");
        mergedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        PolicyPackContentResponse? merged = await mergedResponse.Content.ReadFromJsonAsync<PolicyPackContentResponse>(JsonOptions);
        merged!.ComplianceRuleKeys.Should().BeEquivalentTo("merge-key-a", "merge-key-b");
        return;

        async Task<Guid> CreatePackAsync(string name, string complianceKey)
        {
            string contentJson = $$"""
                                   {
                                     "complianceRuleIds": [],
                                     "complianceRuleKeys": [ "{{complianceKey}}" ],
                                     "alertRuleIds": [],
                                     "compositeAlertRuleIds": [],
                                     "advisoryDefaults": {},
                                     "metadata": {}
                                   }
                                   """;

            HttpResponseMessage createResponse = await Client.PostAsync(
                "/v1/policy-packs",
                JsonContent(
                    new
                    {
                        name,
                        description = "",
                        packType = "ProjectCustom",
                        initialContentJson = contentJson,
                    }));
            createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            PolicyPackResponse? created = await createResponse.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);
            return created!.PolicyPackId;
        }
    }

    private sealed class PolicyPackResponse
    {
        public Guid PolicyPackId
        {
            get; init;
        }
        public string Name { get; init; } = "";
    }

    private sealed class PolicyPackVersionResponse
    {
        public Guid PolicyPackVersionId
        {
            get; init;
        }
        public string Version { get; init; } = "";
        public string ContentJson { get; init; } = "";
        public bool IsPublished
        {
            get; init;
        }
    }

    private sealed class EffectivePolicyPackSetResponse
    {
        public List<ResolvedPackResponse> Packs { get; init; } = [];
    }

    [UsedImplicitly]
    private sealed class ResolvedPackResponse
    {
        [UsedImplicitly]
        public Guid PolicyPackId
        {
            get;
        }
        public static string Version => "";
    }

    private sealed class PolicyPackContentResponse
    {
        public List<string> ComplianceRuleKeys { get; init; } = [];
        public List<Guid> AlertRuleIds { get; init; } = [];
        public Dictionary<string, string> AdvisoryDefaults { get; init; } = [];
        public Dictionary<string, string> Metadata { get; init; } = [];
    }
}
