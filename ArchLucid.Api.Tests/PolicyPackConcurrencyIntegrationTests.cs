using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Governance.PolicyPacks;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Parallel <c>POST /v1/policy-packs</c> with identical name in the same scope: the model does not enforce a unique
///     name, so all writers can succeed. With <c>ArchLucid:StorageProvider=InMemory</c> (default
///     <see cref="ArchLucidApiFactory" />), packs live in the in-process store â€” list by HTTP, not <c>dbo</c> probes.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class PolicyPackConcurrencyIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(null) }
    };

    private static StringContent JsonContent(object value)
    {
        string json = JsonSerializer.Serialize(value, JsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    [SkippableFact]
    public async Task Five_parallel_creates_with_same_name_produce_five_distinct_packs_visible_in_list()
    {
        await using ArchLucidApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        string packName = "Parallel create same name " + Guid.NewGuid().ToString("N");
        const string contentJson = """
                                   {
                                     "complianceRuleIds": [],
                                     "complianceRuleKeys": [],
                                     "alertRuleIds": [],
                                     "compositeAlertRuleIds": [],
                                     "advisoryDefaults": {},
                                     "metadata": { "concurrency": "policy-pack-create" }
                                   }
                                   """;

        object body = new
        {
            name = packName,
            description = "concurrency",
            packType = "ProjectCustom",
            initialContentJson = contentJson
        };

        const int parallel = 5;
        Task<HttpResponseMessage>[] tasks = new Task<HttpResponseMessage>[parallel];

        for (int i = 0; i < parallel; i++)
        {
            tasks[i] = client.PostAsync("/v1/policy-packs", JsonContent(body));
        }

        HttpResponseMessage[] responses = await Task.WhenAll(tasks);
        try
        {
            HashSet<Guid> packIds = [];

            foreach (HttpResponseMessage response in responses)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                PolicyPack? created =
                    await response.Content.ReadFromJsonAsync<PolicyPack>(JsonOptions, CancellationToken.None);
                created.Should().NotBeNull();
                _ = packIds.Add(created.PolicyPackId);
            }

            packIds.Count.Should().Be(parallel, "each parallel create should mint a new PolicyPackId");

            HttpResponseMessage listResponse = await client.GetAsync("/v1/policy-packs", CancellationToken.None);
            listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            IReadOnlyList<PolicyPack>? list =
                await listResponse.Content.ReadFromJsonAsync<IReadOnlyList<PolicyPack>>(
                    JsonOptions,
                    CancellationToken.None);
            list.Should().NotBeNull();
            IReadOnlyList<PolicyPack> inScope = list
                .Where(
                    p => p.TenantId == ScopeIds.DefaultTenant
                    && p.WorkspaceId == ScopeIds.DefaultWorkspace
                    && p.ProjectId == ScopeIds.DefaultProject
                    && string.Equals(p.Name, packName, StringComparison.Ordinal))
                .ToList();

            inScope.Count.Should().Be(parallel);
        }
        finally
        {
            foreach (HttpResponseMessage response in responses)
            {
                response.Dispose();
            }
        }
    }
}
