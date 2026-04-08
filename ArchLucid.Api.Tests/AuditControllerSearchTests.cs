using System.Net.Http.Json;
using System.Reflection;

using ArchLucid.Core.Audit;
using ArchLucid.Persistence.Audit;

using FluentAssertions;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>Tests for <c>GET /v1/audit/search</c> and <c>GET /v1/audit/event-types</c>.</summary>
[Trait("Category", "Integration")]
public sealed class AuditControllerSearchTests
{
    [Fact]
    public async Task SearchAudit_WithEventType_PassesFilterToRepo()
    {
        await using AuditControllerSearchApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        Mock<IAuditRepository> repo = factory.AuditRepositoryMock;
        repo
            .Setup(r => r.GetFilteredAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<AuditEventFilter>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        HttpResponseMessage response = await client.GetAsync("/v1/audit/search?eventType=RunStarted&take=50");

        response.EnsureSuccessStatusCode();
        repo.Verify(
            r => r.GetFilteredAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.Is<AuditEventFilter>(f => f.EventType == "RunStarted" && f.Take == 50),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchAudit_ClampsTake()
    {
        await using AuditControllerSearchApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        Mock<IAuditRepository> repo = factory.AuditRepositoryMock;
        repo
            .Setup(r => r.GetFilteredAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<AuditEventFilter>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        HttpResponseMessage response = await client.GetAsync("/v1/audit/search?take=99999");

        response.EnsureSuccessStatusCode();
        repo.Verify(
            r => r.GetFilteredAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.Is<AuditEventFilter>(f => f.Take == 500),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetEventTypes_ReturnsAllConstants()
    {
        await using ArchLucidApiFactory plain = new();
        HttpClient client = plain.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/v1/audit/event-types");
        response.EnsureSuccessStatusCode();

        List<string>? types = await response.Content.ReadFromJsonAsync<List<string>>();
        types.Should().NotBeNull();

        int expected = typeof(AuditEventTypes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Count(static f => f is { IsLiteral: true, FieldType: { } t } && t == typeof(string));

        types!.Count.Should().Be(expected);
        types.Should().Contain(AuditEventTypes.CircuitBreakerStateTransition);
    }
}
