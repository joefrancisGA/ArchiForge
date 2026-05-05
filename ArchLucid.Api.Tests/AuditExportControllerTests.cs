using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

using ArchLucid.Core.Audit;
using ArchLucid.Core.Tenancy;
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

    [SkippableFact]
    public async Task ExportAudit_AsJson_ReturnsArray()
    {
        await using AuditControllerSearchApiFactory factory = new();
        HttpClient client = await CreateEnterpriseAuditClientAsync(factory);
        Mock<IAuditRepository> repo = factory.AuditRepositoryMock;

        AuditEvent evt = new()
        {
            EventId = Guid.Parse("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1"),
            OccurredUtc = DateTime.Parse("2026-01-01T12:00:00Z", null, DateTimeStyles.RoundtripKind),
            EventType = "RunCreated",
            ActorUserId = "u1",
            ActorUserName = "User One",
            TenantId = Guid.Empty,
            WorkspaceId = Guid.Empty,
            ProjectId = Guid.Empty,
            DataJson = "{}"
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
            .ReturnsAsync([evt]);

        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response = await client.GetAsync(ExportUri);
        response.EnsureSuccessStatusCode();

        JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        doc.RootElement.GetArrayLength().Should().Be(1);
        doc.RootElement[0].GetProperty("eventId").GetString().Should().Be("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1");
    }

    [SkippableFact]
    public async Task ExportAudit_AsCsv_NegotiatesTextCsv_AndAttachment()
    {
        await using AuditControllerSearchApiFactory factory = new();
        HttpClient client = await CreateEnterpriseAuditClientAsync(factory);
        Mock<IAuditRepository> repo = factory.AuditRepositoryMock;

        AuditEvent evt = new()
        {
            EventId = Guid.Parse("c1c1c1c1-c1c1-c1c1-c1c1-c1c1c1c1c1c1"),
            OccurredUtc = DateTime.Parse("2026-01-01T06:00:00Z", null, DateTimeStyles.RoundtripKind),
            EventType = "T",
            ActorUserId = "a",
            ActorUserName = "A",
            TenantId = Guid.Empty,
            WorkspaceId = Guid.Empty,
            ProjectId = Guid.Empty,
            RunId = Guid.Parse("d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1"),
            CorrelationId = "corr",
            DataJson = "{\"x\":1}"
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
            .ReturnsAsync([evt]);

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

    [SkippableFact]
    public async Task ExportAudit_AsCef_ReturnsTextPlain_AndCefLine()
    {
        await using AuditControllerSearchApiFactory factory = new();
        HttpClient client = await CreateEnterpriseAuditClientAsync(factory);
        Mock<IAuditRepository> repo = factory.AuditRepositoryMock;

        AuditEvent evt = new()
        {
            EventId = Guid.Parse("e1e1e1e1-e1e1-e1e1-e1e1-e1e1e1e1e1e1"),
            OccurredUtc = DateTime.Parse("2026-01-01T09:00:00Z", null, DateTimeStyles.RoundtripKind),
            EventType = "RunExported",
            ActorUserId = "actor",
            ActorUserName = "Actor",
            TenantId = Guid.Empty,
            WorkspaceId = Guid.Empty,
            ProjectId = Guid.Empty,
            RunId = Guid.Parse("f1f1f1f1-f1f1-f1f1-f1f1-f1f1f1f1f1f1"),
            CorrelationId = "c-cef",
            DataJson = "{\"k\":2}"
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
            .ReturnsAsync([evt]);

        Uri uri = new("/v1/audit/export?fromUtc=2026-01-01T00:00:00.0000000Z&toUtc=2026-01-02T00:00:00.0000000Z&format=cef", UriKind.Relative);
        HttpResponseMessage response = await client.GetAsync(uri);
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");

        string? disposition = response.Content.Headers.ContentDisposition?.ToString();
        disposition.Should().NotBeNullOrWhiteSpace();
        disposition.ToLowerInvariant().Should().Contain("attachment");
        disposition.ToLowerInvariant().Should().Contain(".cef");

        string body = await response.Content.ReadAsStringAsync();
        body.Should().StartWith("CEF:0|ArchLucid|ArchLucid API|1.0|");
        body.Should().Contain("eventId=e1e1e1e1-e1e1-e1e1-e1e1-e1e1e1e1e1e1");
    }

    [SkippableFact]
    public async Task ExportAudit_InvalidRange_Returns400()
    {
        await using AuditControllerSearchApiFactory factory = new();
        HttpClient client = await CreateEnterpriseAuditClientAsync(factory);

        HttpResponseMessage response = await client.GetAsync(
            "/v1/audit/export?fromUtc=2026-02-01T00:00:00.0000000Z&toUtc=2026-01-01T00:00:00.0000000Z");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [SkippableFact]
    public async Task ExportAudit_RangeOver90Days_Returns400()
    {
        await using AuditControllerSearchApiFactory factory = new();
        HttpClient client = await CreateEnterpriseAuditClientAsync(factory);

        HttpResponseMessage response = await client.GetAsync(
            "/v1/audit/export?fromUtc=2026-01-01T00:00:00.0000000Z&toUtc=2026-04-02T00:00:00.0000000Z");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [SkippableFact]
    public async Task ExportAudit_ClampsMaxRows_BeforeCallingRepository()
    {
        await using AuditControllerSearchApiFactory factory = new();
        HttpClient client = await CreateEnterpriseAuditClientAsync(factory);
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

    private static async Task<HttpClient> CreateEnterpriseAuditClientAsync(AuditControllerSearchApiFactory factory)
    {
        await CommercialTierIntegrationTestTenant.SetDefaultScopedTenantTierAsync(factory, TenantTier.Enterprise);

        return factory.CreateClient();
    }
}
