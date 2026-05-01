using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Parallel publish of the same policy pack version converges on one published
///     <see cref="PolicyPackVersionResponse" />.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class PolicyPackPublishConcurrencyIntegrationTests
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

    private static async Task WithIsolatedFactory(Func<HttpClient, Task> act)
    {
        await using ArchLucidApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);
        await act(client);
    }

    [SkippableFact]
    public async Task Eight_parallel_publish_posts_single_policy_pack_version_row()
    {
        await WithIsolatedFactory(async client =>
        {
            HttpResponseMessage createResponse = await client.PostAsync(
                "/v1/policy-packs",
                JsonContent(
                    new
                    {
                        name = "Concurrency publish pack",
                        description = "",
                        packType = "ProjectCustom",
                        initialContentJson = "{}"
                    }));
            createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            PolicyPackResponse? created =
                await createResponse.Content.ReadFromJsonAsync<PolicyPackResponse>(JsonOptions);
            Guid packId = created!.PolicyPackId;

            object body = new
            {
                version = "2.1.0-conc",
                contentJson = """{"metadata":{"k":"conc8"}}"""
            };

            const int parallel = 8;
            Task<HttpResponseMessage>[] tasks = new Task<HttpResponseMessage>[parallel];

            for (int i = 0; i < parallel; i++)
            {
                tasks[i] = client.PostAsync($"/v1/policy-packs/{packId}/publish", JsonContent(body));
            }

            HttpResponseMessage[] responses = await Task.WhenAll(tasks);

            try
            {
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
                versions.Count(static v => v.Version == "2.1.0-conc").Should().Be(1);
            }
            finally
            {
                foreach (HttpResponseMessage response in responses)
                {
                    response.Dispose();
                }
            }
        });
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
}
