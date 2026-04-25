using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Api.Routing;
using ArchLucid.Decisioning.Governance.PolicyPacks;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Integration tests: Policy Packs (HTTP host, database, or cross-component).
///     Each test uses a dedicated <see cref="ArchLucidApiFactory" /> (ephemeral SQL database) so assignments and
///     effective-content merges do not leak across tests (see <c>ArchitectureControllerTests</c>).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class PolicyPacksIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter(null, true) }
    };

    private static StringContent JsonContent(object value)
    {
        string json = JsonSerializer.Serialize(value, JsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static async Task WithIsolatedFactory(Func<HttpClient, Task> act)
    {
        await using ArchLucidApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        await act(client);
    }

    [Fact]
    public async Task PolicyPack_CreateAssignEffective_Lifecycle()
    {
        await WithIsolatedFactory(async client =>
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

            HttpResponseMessage createResponse = await client.PostAsync(
                "/v1/policy-packs",
                JsonContent(
                    new
                    {
                        name = "Integration test pack",
                        description = "lifecycle",
                        packType = "ProjectCustom",
                        initialContentJson = contentJson
                    }));

            createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            PolicyPackResponse? created =
                await createResponse.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);
            created.Should().NotBeNull();
            Guid packId = created.PolicyPackId;

            HttpResponseMessage assignResponse = await client.PostAsync(
                $"/v1/policy-packs/{packId}/assign",
                JsonContent(new { version = "1.0.0" }));

            assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            HttpResponseMessage effectiveResponse = await client.GetAsync("/v1/policy-packs/effective");
            effectiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            EffectivePolicyPackSet? effective =
                await effectiveResponse.Content.ReadFromJsonAsync<EffectivePolicyPackSet>(JsonOptions);
            effective.Should().NotBeNull();
            ResolvedPolicyPack? resolved = effective.Packs.SingleOrDefault(p => p.PolicyPackId == packId);
            resolved.Should().NotBeNull("created pack should appear in effective set for current scope");
            resolved.Version.Should().Be("1.0.0");

            HttpResponseMessage mergedResponse = await client.GetAsync("/v1/policy-packs/effective-content");
            mergedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            PolicyPackContentResponse? merged =
                await mergedResponse.Content.ReadFromJsonAsync<PolicyPackContentResponse>(JsonOptions);
            merged.Should().NotBeNull();
            merged.AdvisoryDefaults.Should().ContainKey("scanDepth");
            merged.AdvisoryDefaults["scanDepth"].Should().Be("standard");
            merged.Metadata.Should().ContainKey("tier");
        });
    }

    [Fact]
    public async Task ArchiveAssignment_Removes_pack_from_effective_set()
    {
        await WithIsolatedFactory(async client =>
        {
            string contentJson = """
                                 {
                                   "complianceRuleIds": [],
                                   "complianceRuleKeys": [],
                                   "alertRuleIds": [],
                                   "compositeAlertRuleIds": [],
                                   "advisoryDefaults": {},
                                   "metadata": { "archiveProbe": "yes" }
                                 }
                                 """;

            HttpResponseMessage createResponse = await client.PostAsync(
                "/v1/policy-packs",
                JsonContent(
                    new
                    {
                        name = "Archive probe pack",
                        description = "",
                        packType = "ProjectCustom",
                        initialContentJson = contentJson
                    }));

            createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            PolicyPackResponse? created =
                await createResponse.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);
            Guid packId = created!.PolicyPackId;

            HttpResponseMessage assignResponse = await client.PostAsync(
                $"/v1/policy-packs/{packId}/assign",
                JsonContent(new { version = "1.0.0" }));

            assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            PolicyPackAssignment? assignment =
                await assignResponse.Content.ReadFromJsonAsync<PolicyPackAssignment>(JsonOptions);
            assignment.Should().NotBeNull();

            HttpResponseMessage archiveResponse =
                await client.PostAsync($"/v1/policy-packs/assignments/{assignment.AssignmentId}/archive", null);

            archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            HttpResponseMessage effectiveResponse = await client.GetAsync("/v1/policy-packs/effective");
            effectiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            EffectivePolicyPackSet? effective =
                await effectiveResponse.Content.ReadFromJsonAsync<EffectivePolicyPackSet>(JsonOptions);
            effective.Should().NotBeNull();
            effective.Packs.Should().NotContain(p => p.PolicyPackId == packId);
        });
    }

    [Fact]
    public async Task EffectiveContent_MergesComplianceRuleKeys_FromAssignedPack()
    {
        await WithIsolatedFactory(async client =>
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

            HttpResponseMessage createResponse = await client.PostAsync(
                "/v1/policy-packs",
                JsonContent(
                    new
                    {
                        name = "Compliance keys pack",
                        description = "",
                        packType = "ProjectCustom",
                        initialContentJson = contentJson
                    }));
            createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            PolicyPackResponse? created =
                await createResponse.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);
            Guid packId = created!.PolicyPackId;

            HttpResponseMessage assignResponse = await client.PostAsync(
                $"/v1/policy-packs/{packId}/assign",
                JsonContent(new { version = "1.0.0" }));
            assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            HttpResponseMessage mergedResponse = await client.GetAsync("/v1/policy-packs/effective-content");
            mergedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            PolicyPackContentResponse? merged =
                await mergedResponse.Content.ReadFromJsonAsync<PolicyPackContentResponse>(JsonOptions);
            merged.Should().NotBeNull();
            merged.ComplianceRuleKeys.Should().BeEquivalentTo("rule-alpha", "rule-beta");
        });
    }

    [Fact]
    public async Task EffectiveContent_MergesAlertRuleIds_FromAssignedPack()
    {
        await WithIsolatedFactory(async client =>
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

            HttpResponseMessage createResponse = await client.PostAsync(
                "/v1/policy-packs",
                JsonContent(
                    new
                    {
                        name = "Alert ids pack",
                        description = "",
                        packType = "ProjectCustom",
                        initialContentJson = contentJson
                    }));
            createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            PolicyPackResponse? created =
                await createResponse.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);
            Guid packId = created!.PolicyPackId;

            HttpResponseMessage assignResponse = await client.PostAsync(
                $"/v1/policy-packs/{packId}/assign",
                JsonContent(new { version = "1.0.0" }));
            assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            HttpResponseMessage mergedResponse = await client.GetAsync("/v1/policy-packs/effective-content");
            mergedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            PolicyPackContentResponse? merged =
                await mergedResponse.Content.ReadFromJsonAsync<PolicyPackContentResponse>(JsonOptions);
            merged.Should().NotBeNull();
            merged.AlertRuleIds.Should().BeEquivalentTo([idA, idB]);
        });
    }

    [Fact]
    public async Task PublishSameVersion_EightParallelPosts_YieldsSinglePublishedRow()
    {
        await WithIsolatedFactory(async client =>
        {
            HttpResponseMessage createResponse = await client.PostAsync(
                "/v1/policy-packs",
                JsonContent(
                    new
                    {
                        name = "Parallel publish pack",
                        description = "",
                        packType = "ProjectCustom",
                        initialContentJson = "{}"
                    }));
            createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            PolicyPackResponse? created =
                await createResponse.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);
            Guid packId = created!.PolicyPackId;

            object body = new { version = "2.0.0", contentJson = """{"metadata":{"k":"parallel"}}""" };

            const int parallel = 8;
            Task<HttpResponseMessage>[] tasks = new Task<HttpResponseMessage>[parallel];

            for (int i = 0; i < parallel; i++)
            {
                tasks[i] = client.PostAsync($"/v1/policy-packs/{packId}/publish", JsonContent(body));
            }

            HttpResponseMessage[] responses = await Task.WhenAll(tasks);

            Guid? versionId = null;

            foreach (HttpResponseMessage response in responses)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                PolicyPackVersionResponse? row =
                    await response.Content.ReadFromJsonAsync<PolicyPackVersionResponse>(JsonOptions);
                row.Should().NotBeNull();

                if (versionId is null)
                {
                    versionId = row.PolicyPackVersionId;
                }
                else
                {
                    row.PolicyPackVersionId.Should().Be(versionId.Value);
                }
            }

            HttpResponseMessage list = await client.GetAsync($"/v1/policy-packs/{packId}/versions");
            list.StatusCode.Should().Be(HttpStatusCode.OK);
            List<PolicyPackVersionResponse>? versions =
                await list.Content.ReadFromJsonAsync<List<PolicyPackVersionResponse>>(JsonOptions);
            versions.Should().NotBeNull();
            versions.Count(v => v.Version == "2.0.0").Should().Be(1);
        });
    }

    [Fact]
    public async Task PublishSameVersionTwice_IsIdempotent_SingleVersionRow()
    {
        await WithIsolatedFactory(async client =>
        {
            HttpResponseMessage createResponse = await client.PostAsync(
                "/v1/policy-packs",
                JsonContent(
                    new
                    {
                        name = "Idempotent publish pack",
                        description = "",
                        packType = "ProjectCustom",
                        initialContentJson = "{}"
                    }));
            createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            PolicyPackResponse? created =
                await createResponse.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);
            Guid packId = created!.PolicyPackId;

            var bodyA = new { version = "1.0.0", contentJson = """{"metadata":{"k":"a"}}""" };
            var bodyB = new { version = "1.0.0", contentJson = """{"metadata":{"k":"b"}}""" };

            HttpResponseMessage p1 = await client.PostAsync($"/v1/policy-packs/{packId}/publish", JsonContent(bodyA));
            p1.StatusCode.Should().Be(HttpStatusCode.OK);
            PolicyPackVersionResponse? v1 = await p1.Content.ReadFromJsonAsync<PolicyPackVersionResponse>(JsonOptions);
            v1.Should().NotBeNull();

            HttpResponseMessage p2 = await client.PostAsync($"/v1/policy-packs/{packId}/publish", JsonContent(bodyB));
            p2.StatusCode.Should().Be(HttpStatusCode.OK);
            PolicyPackVersionResponse? v2 = await p2.Content.ReadFromJsonAsync<PolicyPackVersionResponse>(JsonOptions);
            v2!.PolicyPackVersionId.Should().Be(v1.PolicyPackVersionId);

            HttpResponseMessage list = await client.GetAsync($"/v1/policy-packs/{packId}/versions");
            list.StatusCode.Should().Be(HttpStatusCode.OK);
            List<PolicyPackVersionResponse>? versions =
                await list.Content.ReadFromJsonAsync<List<PolicyPackVersionResponse>>(JsonOptions);
            versions.Should().NotBeNull();
            versions.Count(x => x.Version == "1.0.0").Should().Be(1);
            versions.Single(x => x.Version == "1.0.0").ContentJson.Should().Contain("\"k\":\"b\"");
        });
    }

    [Fact]
    public async Task ListVersions_AfterCreate_IncludesInitialVersion()
    {
        await WithIsolatedFactory(async client =>
        {
            HttpResponseMessage createResponse = await client.PostAsync(
                "/v1/policy-packs",
                JsonContent(
                    new
                    {
                        name = "Versions list pack",
                        description = "",
                        packType = "ProjectCustom",
                        initialContentJson = "{}"
                    }));
            PolicyPackResponse? created =
                await createResponse.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);
            Guid packId = created!.PolicyPackId;

            HttpResponseMessage list = await client.GetAsync($"/v1/policy-packs/{packId}/versions");
            list.StatusCode.Should().Be(HttpStatusCode.OK);
            List<PolicyPackVersionResponse>? versions =
                await list.Content.ReadFromJsonAsync<List<PolicyPackVersionResponse>>(JsonOptions);
            versions.Should().ContainSingle(v => v.Version == "1.0.0" && v.IsPublished == false);
        });
    }

    [Fact]
    public async Task ArchiveAssignment_unknown_assignment_returns_404()
    {
        await WithIsolatedFactory(async client =>
        {
            HttpResponseMessage res =
                await client.PostAsync($"/v1/policy-packs/assignments/{Guid.NewGuid()}/archive", null);

            res.StatusCode.Should().Be(HttpStatusCode.NotFound);
        });
    }

    [Fact]
    public async Task AssignUnknownVersion_Returns404()
    {
        await WithIsolatedFactory(async client =>
        {
            HttpResponseMessage createResponse = await client.PostAsync(
                "/v1/policy-packs",
                JsonContent(
                    new
                    {
                        name = "Assign 404 pack",
                        description = "",
                        packType = "ProjectCustom",
                        initialContentJson = "{}"
                    }));
            PolicyPackResponse? created =
                await createResponse.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);
            Guid packId = created!.PolicyPackId;

            HttpResponseMessage assign = await client.PostAsync(
                $"/v1/policy-packs/{packId}/assign",
                JsonContent(new { version = "99.0.0" }));

            assign.StatusCode.Should().Be(HttpStatusCode.NotFound);
            string text = await assign.Content.ReadAsStringAsync();
            using JsonDocument problem = JsonDocument.Parse(text);
            JsonElement root = problem.RootElement;
            string? typeString = root.TryGetProperty("type", out JsonElement camel)
                ? camel.GetString()
                : root.GetProperty("Type").GetString();
            typeString.Should().Contain("policy-pack-version-not-found");
        });
    }

    [Fact]
    public async Task GovernanceResolution_Get_ReturnsNotesAndEffectiveContent()
    {
        await WithIsolatedFactory(async client =>
        {
            HttpResponseMessage createResponse = await client.PostAsync(
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
                                             """
                    }));
            createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            PolicyPackResponse? created =
                await createResponse.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);
            Guid packId = created!.PolicyPackId;

            (await client.PostAsync(
                    $"/v1/policy-packs/{packId}/assign",
                    JsonContent(new { version = "1.0.0", scopeLevel = "Project", isPinned = false })))
                .StatusCode.Should().Be(HttpStatusCode.OK);

            HttpResponseMessage res = await client.GetAsync($"/{ApiV1Routes.GovernanceResolution}");
            res.StatusCode.Should().Be(HttpStatusCode.OK);
            using JsonDocument doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            JsonElement root = doc.RootElement;
            root.GetProperty("notes").EnumerateArray().Should().NotBeEmpty();
            root.GetProperty("decisions").EnumerateArray().Should().NotBeEmpty();
            root.GetProperty("effectiveContent").GetProperty("metadata").GetProperty("k").GetString().Should().Be("v");
        });
    }

    [Fact]
    public async Task EffectiveContent_MergesAdvisoryDefaults_FromTwoAssignedPacks()
    {
        await WithIsolatedFactory(async client =>
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

            HttpResponseMessage createA = await client.PostAsync(
                "/v1/policy-packs",
                JsonContent(new
                {
                    name = "Two-pack merge A",
                    description = "",
                    packType = "ProjectCustom",
                    initialContentJson = contentA
                }));
            createA.StatusCode.Should().Be(HttpStatusCode.OK);
            PolicyPackResponse? packA = await createA.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);

            HttpResponseMessage createB = await client.PostAsync(
                "/v1/policy-packs",
                JsonContent(new
                {
                    name = "Two-pack merge B",
                    description = "",
                    packType = "ProjectCustom",
                    initialContentJson = contentB
                }));
            createB.StatusCode.Should().Be(HttpStatusCode.OK);
            PolicyPackResponse? packB = await createB.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);

            (await client.PostAsync(
                    $"/v1/policy-packs/{packA!.PolicyPackId}/assign",
                    JsonContent(new { version = "1.0.0" })))
                .StatusCode.Should().Be(HttpStatusCode.OK);
            (await client.PostAsync(
                    $"/v1/policy-packs/{packB!.PolicyPackId}/assign",
                    JsonContent(new { version = "1.0.0" })))
                .StatusCode.Should().Be(HttpStatusCode.OK);

            HttpResponseMessage mergedResponse = await client.GetAsync("/v1/policy-packs/effective-content");
            mergedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            PolicyPackContentResponse? merged =
                await mergedResponse.Content.ReadFromJsonAsync<PolicyPackContentResponse>(JsonOptions);
            merged.Should().NotBeNull();
            merged.AdvisoryDefaults.Should().ContainKeys("scanDepth", "notifyChannel");
            merged.AdvisoryDefaults["scanDepth"].Should().Be("deep");
            merged.AdvisoryDefaults["notifyChannel"].Should().Be("email");
            merged.Metadata.Should().ContainKey("mergeTier");
            merged.Metadata["mergeTier"].Should().Be("baseline");
            merged.Metadata.Should().ContainKey("packRole");
            merged.Metadata["packRole"].Should().Be("overlay");
        });
    }

    [Fact]
    public async Task EffectiveContent_UnionsComplianceRuleKeys_FromTwoAssignments()
    {
        await WithIsolatedFactory(async client =>
        {
            Guid packA = await CreatePackAsync(client, "merge-pack-a", "merge-key-a");
            Guid packB = await CreatePackAsync(client, "merge-pack-b", "merge-key-b");

            (await client.PostAsync(
                    $"/v1/policy-packs/{packA}/assign",
                    JsonContent(new { version = "1.0.0" })))
                .StatusCode.Should().Be(HttpStatusCode.OK);
            (await client.PostAsync(
                    $"/v1/policy-packs/{packB}/assign",
                    JsonContent(new { version = "1.0.0" })))
                .StatusCode.Should().Be(HttpStatusCode.OK);

            HttpResponseMessage effectiveResponse = await client.GetAsync("/v1/policy-packs/effective");
            effectiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            EffectivePolicyPackSet? effectiveSet =
                await effectiveResponse.Content.ReadFromJsonAsync<EffectivePolicyPackSet>(JsonOptions);
            effectiveSet.Should().NotBeNull();
            Guid[] createdPackIds = [packA, packB];
            effectiveSet.Packs.Where(p => createdPackIds.Contains(p.PolicyPackId)).Should().HaveCount(2);

            HttpResponseMessage mergedResponse = await client.GetAsync("/v1/policy-packs/effective-content");
            mergedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            PolicyPackContentResponse? merged =
                await mergedResponse.Content.ReadFromJsonAsync<PolicyPackContentResponse>(JsonOptions);
            merged!.ComplianceRuleKeys.Should().Contain("merge-key-a", "merge-key-b");
        });

        static async Task<Guid> CreatePackAsync(HttpClient http, string name, string complianceKey)
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

            HttpResponseMessage createResponse = await http.PostAsync(
                "/v1/policy-packs",
                JsonContent(
                    new { name, description = "", packType = "ProjectCustom", initialContentJson = contentJson }));
            createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            PolicyPackResponse? created =
                await createResponse.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);
            return created!.PolicyPackId;
        }
    }

    private sealed class PolicyPackResponse
    {
        public Guid PolicyPackId
        {
            get;
            init;
        }

        public string Name
        {
            get;
            init;
        } = "";
    }

    private sealed class PolicyPackVersionResponse
    {
        public Guid PolicyPackVersionId
        {
            get;
            init;
        }

        public string Version
        {
            get;
            init;
        } = "";

        public string ContentJson
        {
            get;
            init;
        } = "";

        public bool IsPublished
        {
            get;
            init;
        }
    }

    private sealed class PolicyPackContentResponse
    {
        public List<string> ComplianceRuleKeys
        {
            get;
            init;
        } = [];

        public List<Guid> AlertRuleIds
        {
            get;
            init;
        } = [];

        public Dictionary<string, string> AdvisoryDefaults
        {
            get;
            init;
        } = [];

        public Dictionary<string, string> Metadata
        {
            get;
            init;
        } = [];
    }
}
