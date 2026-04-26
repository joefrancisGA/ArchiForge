using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Api.Tests.TestDtos;
using ArchLucid.Contracts.Pilots;
using ArchLucid.Core.Audit;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Pilot execute path: <c>X-ArchLucid-Pilot-Try-Real-Mode</c> headers drive first-real-value audit + counters.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class FirstRealValuePilotExecuteIntegrationTests
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

    [Fact]
    public async Task ExecuteRun_WithPilotTryRealHeader_emitsFirstRealValueAuditEvents()
    {
        await using ArchLucidApiFactory factory = new();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage createResponse = await client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-PILOT-REAL-1")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created =
            await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        created.Should().NotBeNull();
        string runId = created.Run.RunId;
        bool parsed = Guid.TryParse(runId, out Guid runGuid);
        parsed.Should().BeTrue();

        using HttpRequestMessage execute = new(HttpMethod.Post, $"/v1/architecture/run/{runId}/execute");
        execute.Headers.TryAddWithoutValidation(PilotTryRealModeHeaders.PilotTryRealMode, "1");

        HttpResponseMessage executeResponse = await client.SendAsync(execute);
        executeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage auditStarted = await client.GetAsync(
            $"/v1/audit/search?eventType={Uri.EscapeDataString(AuditEventTypes.FirstRealValueRunStarted)}&runId={runGuid}&take=10");

        auditStarted.StatusCode.Should().Be(HttpStatusCode.OK);
        List<AuditEvent>? startedEvents = await auditStarted.Content.ReadFromJsonAsync<List<AuditEvent>>(JsonOptions);
        startedEvents.Should().NotBeNull();
        startedEvents.Should().Contain(e => e.EventType == AuditEventTypes.FirstRealValueRunStarted);

        HttpResponseMessage auditCompleted = await client.GetAsync(
            $"/v1/audit/search?eventType={Uri.EscapeDataString(AuditEventTypes.FirstRealValueRunCompleted)}&runId={runGuid}&take=10");

        auditCompleted.StatusCode.Should().Be(HttpStatusCode.OK);
        List<AuditEvent>? completedEvents = await auditCompleted.Content.ReadFromJsonAsync<List<AuditEvent>>(JsonOptions);
        completedEvents.Should().NotBeNull();
        completedEvents.Should().Contain(e => e.EventType == AuditEventTypes.FirstRealValueRunCompleted);
    }
}
