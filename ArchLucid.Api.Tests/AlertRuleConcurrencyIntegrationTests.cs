using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Api.Routing;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Alerts;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Parallel <c>POST /v1/alert-rules</c> with identical <see cref="AlertRule.Name" />: there is no uniqueness guard on
///     the name. With in-memory storage, rules are not in <c>dbo.AlertRules</c> â€” the list API is the ground truth.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class AlertRuleConcurrencyIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter(null) }
    };

    [SkippableFact]
    public async Task Five_parallel_creates_with_same_name_persist_five_distinct_rule_rows_in_list()
    {
        await using ArchLucidApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        string name = "Concurrency same name " + Guid.NewGuid().ToString("N");
        object body = new
        {
            name,
            ruleType = AlertRuleType.CriticalRecommendationCount,
            severity = AlertSeverity.Warning,
            thresholdValue = 0m,
            isEnabled = true,
            targetChannelType = "DigestOnly"
        };

        const int parallel = 5;
        Task<HttpResponseMessage>[] tasks = new Task<HttpResponseMessage>[parallel];

        for (int i = 0; i < parallel; i++)
        {
            tasks[i] = client.PostAsJsonAsync(
                $"/{ApiV1Routes.AlertRules}",
                body,
                JsonOptions,
                CancellationToken.None);
        }

        HttpResponseMessage[] responses = await Task.WhenAll(tasks);

        try
        {
            HashSet<Guid> ruleIds = [];

            foreach (HttpResponseMessage response in responses)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                AlertRule? created = await response.Content
                    .ReadFromJsonAsync<AlertRule>(JsonOptions, CancellationToken.None);
                created.Should().NotBeNull();
                _ = ruleIds.Add(created.RuleId);
            }

            ruleIds.Count.Should().Be(parallel);

            HttpResponseMessage listResponse = await client.GetAsync(
                $"/{ApiV1Routes.AlertRules}",
                CancellationToken.None);
            listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            IReadOnlyList<AlertRule>? list =
                await listResponse.Content.ReadFromJsonAsync<IReadOnlyList<AlertRule>>(
                    JsonOptions,
                    CancellationToken.None);
            list.Should().NotBeNull();
            IReadOnlyList<AlertRule> inScope = list
                .Where(r => r.TenantId == ScopeIds.DefaultTenant
                            && r.WorkspaceId == ScopeIds.DefaultWorkspace
                            && r.ProjectId == ScopeIds.DefaultProject
                            && string.Equals(r.Name, name, StringComparison.Ordinal))
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
