using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using ArchLucid.Contracts.Trust;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Admin publication of <see cref="ArchLucid.Core.Audit.AuditEventTypes.SecurityAssessmentPublished" /> (trust UI +
///     SIEM signal).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
public sealed class SecurityTrustPublicationControllerIntegrationTests(ArchLucidApiFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Post_publications_returns_204_and_audit_search_finds_event()
    {
        SecurityAssessmentPublicationRequest body = new()
        {
            AssessmentCode = "2026-Q2",
            SummaryReference = "docs/security/pen-test-summaries/2026-Q2-REDACTED-SUMMARY.md",
            AssessorDisplayName = "Aeronova Red Team LLC",
            PublishedOn = "2026-07-29"
        };

        HttpResponseMessage post =
            await Client.PostAsJsonAsync("/v1/admin/security-trust/publications", body, JsonOptions);

        post.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage search = await Client.GetAsync(
            "/v1/audit/search?eventType=SecurityAssessmentPublished&take=5");

        search.StatusCode.Should().Be(HttpStatusCode.OK);
        string raw = await search.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(raw);

        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        doc.RootElement.GetArrayLength().Should().BeGreaterThan(0);

        JsonElement first = doc.RootElement[0];
        first.GetProperty("eventType").GetString().Should().Be("SecurityAssessmentPublished");
        first.GetProperty("dataJson").GetString().Should().Contain("2026-Q2");
        first.GetProperty("dataJson").GetString().Should().Contain("2026-07-29");
    }

    [Fact]
    public async Task Post_publications_returns_400_when_published_on_is_not_a_date()
    {
        SecurityAssessmentPublicationRequest body = new()
        {
            AssessmentCode = "2026-Q2",
            SummaryReference = "docs/security/pen-test-summaries/2026-Q2-REDACTED-SUMMARY.md",
            PublishedOn = "not-a-date"
        };

        HttpResponseMessage post =
            await Client.PostAsJsonAsync("/v1/admin/security-trust/publications", body, JsonOptions);

        post.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
