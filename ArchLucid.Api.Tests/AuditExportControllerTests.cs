using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

using ArchLucid.Core.Audit;
using ArchLucid.Persistence.Audit;

using FluentAssertions;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>Tests for <c>GET /v1/audit/export</c> (JSON/CSV negotiation and validation).</summary>
[Trait("Category", "Integration")]
public sealed class AuditExportControllerTests
{
    private static readonly Uri ExportUri = new(
        "/v1/audit/export?fromUtc=2026-01-01T00:00:00.0000000Z&toUtc=2026-01-02T00:00:00.0000000Z",
        UriKind.Relative);

    [Fact]
    public async Task ExportAudit_AsJson_ReturnsArray()
    {
        await using AuditControllerSearchApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        Mock<IAuditRepository> repo = factory.AuditRepositoryMock;

        AuditEvent evt = new()
        {
            EventId = Guid.Parse("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1"),
            OccurredUtc = DateTime.Parse("2026-01-01T12:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind),
            EventType = "RunCreated",
            ActorUserId = "u1",
            ActorUserName = "User One",
            TenantId = Guid.Empty,
            WorkspaceId = Guid.Empty,
            ProjectId = Guid.Empty,
            DataJson = "{}",
        };

        repo
            .Setup(r => r.GetExportAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { evt });

        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response = await client.GetAsync(ExportUri);
        response.EnsureSuccessStatusCode();

        JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        doc.RootElement.GetArrayLength().Should().Be(1);
        doc.RootElement[0].GetProperty("eventId").GetString().Should().Be("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1");
    }

    [Fact]
    public async Task ExportAudit_AsCsv_NegotiatesTextCsv_AndAttachment()
    {
        await using AuditControllerSearchApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        Mock<IAuditRepository> repo = factory.AuditRepositoryMock;

        AuditEvent evt = new()
        {
            EventId = Guid.Parse("c1c1c1c1-c1c1-c1c1-c1c1-c1c1c1c1c1c1"),
            OccurredUtc = DateTime.Parse("2026-01-01T06:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind),
            EventType = "T",
            ActorUserId = "a",
            ActorUserName = "A",
            TenantId = Guid.Empty,
            WorkspaceId = Guid.Empty,
            ProjectId = Guid.Empty,
            RunId = Guid.Parse("d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1"),
            CorrelationId = "corr",
            DataJson = "{\"x\":1}",
        };

        repo
            .Setup(r => r.GetExportAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { evt });

        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/csv"));

        HttpResponseMessage response = await client.GetAsync(ExportUri);
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");

        string? disposition = response.Content.Headers.ContentDisposition?.ToString();
        disposition.Should().NotBeNullOrWhiteSpace();
        string lower = disposition.ToLowerInvariant();
        lower.Should().Contain("attachment");
        lower.Should().Contain("audit-export-");
        lower.Should().Contain(".csv");

        string body = await response.Content.ReadAsStringAsync();
        body.Should().StartWith(
            "EventId,OccurredUtc,EventType,ActorUserId,ActorUserName,RunId,ManifestId,CorrelationId,DataJson");
    }

    [Fact]
    public async Task ExportAudit_InvalidRange_Returns400()
    {
        await using AuditControllerSearchApiFactory factory = new();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/v1/audit/export?fromUtc=2026-02-01T00:00:00.0000000Z&toUtc=2026-01-01T00:00:00.0000000Z");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExportAudit_RangeOver90Days_Returns400()
    {
        await using AuditControllerSearchApiFactory factory = new();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/v1/audit/export?fromUtc=2026-01-01T00:00:00.0000000Z&toUtc=2026-04-02T00:00:00.0000000Z");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExportAudit_ClampsMaxRows_BeforeCallingRepository()
    {
        await using AuditControllerSearchApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        Mock<IAuditRepository> repo = factory.AuditRepositoryMock;
        repo
            .Setup(r => r.GetExportAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        HttpResponseMessage response = await client.GetAsync(
            "/v1/audit/export?fromUtc=2026-01-01T00:00:00.0000000Z&toUtc=2026-01-02T00:00:00.0000000Z&maxRows=9999999");

        response.EnsureSuccessStatusCode();
        repo.Verify(
            r => r.GetExportAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                10_000,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
