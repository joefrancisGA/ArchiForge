using ArchLucid.Core.Metering;
using ArchLucid.Core.Scoping;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Api.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ApiRequestMeteringMiddlewareTests
{
    private static readonly Guid NonEmptyTenant = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static Mock<IOptionsMonitor<MeteringOptions>> CreateMeteringOptions(bool enabled)
    {
        Mock<IOptionsMonitor<MeteringOptions>> options = new();
        options.Setup(metering => metering.CurrentValue).Returns(new MeteringOptions { Enabled = enabled });

        return options;
    }

    private static DefaultHttpContext CreateContext(string path)
    {
        DefaultHttpContext context = new() { Request = { Path = path }, TraceIdentifier = "trace-1" };

        return context;
    }

    [SkippableFact]
    public async Task InvokeAsync_when_metering_disabled_does_not_record()
    {
        Mock<IScopeContextProvider> scopes = new();
        Mock<IUsageMeteringService> meter = new();
        ApiRequestMeteringMiddleware middleware = new(
            scopes.Object,
            meter.Object,
            CreateMeteringOptions(false).Object,
            NullLogger<ApiRequestMeteringMiddleware>.Instance);

        DefaultHttpContext context = CreateContext("/v1/runs");
        scopes.Setup(scopeProvider => scopeProvider.GetCurrentScope())
            .Returns(
                new ScopeContext
                {
                    TenantId = NonEmptyTenant,
                    WorkspaceId = ScopeIds.DefaultWorkspace,
                    ProjectId = ScopeIds.DefaultProject
                });

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        meter.Verify(
            usage =>
                usage.RecordAsync(It.IsAny<UsageEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/openapi/v1.json")]
    [InlineData("/api/v1/runs")]
    public async Task InvokeAsync_when_path_not_version_api_prefix_does_not_record(string path)
    {
        Mock<IScopeContextProvider> scopes = new();
        Mock<IUsageMeteringService> meter = new();
        ApiRequestMeteringMiddleware middleware = new(
            scopes.Object,
            meter.Object,
            CreateMeteringOptions(true).Object,
            NullLogger<ApiRequestMeteringMiddleware>.Instance);

        DefaultHttpContext context = CreateContext(path);
        scopes.Setup(scopeProvider => scopeProvider.GetCurrentScope())
            .Returns(
                new ScopeContext
                {
                    TenantId = NonEmptyTenant,
                    WorkspaceId = ScopeIds.DefaultWorkspace,
                    ProjectId = ScopeIds.DefaultProject
                });

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        meter.Verify(
            usage =>
                usage.RecordAsync(It.IsAny<UsageEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("/v1/health/ready")]
    [InlineData("/v1/swagger/index.html")]
    public async Task InvokeAsync_when_path_contains_health_or_swagger_does_not_record(string path)
    {
        Mock<IScopeContextProvider> scopes = new();
        Mock<IUsageMeteringService> meter = new();
        ApiRequestMeteringMiddleware middleware = new(
            scopes.Object,
            meter.Object,
            CreateMeteringOptions(true).Object,
            NullLogger<ApiRequestMeteringMiddleware>.Instance);

        DefaultHttpContext context = CreateContext(path);
        scopes.Setup(scopeProvider => scopeProvider.GetCurrentScope())
            .Returns(
                new ScopeContext
                {
                    TenantId = NonEmptyTenant,
                    WorkspaceId = ScopeIds.DefaultWorkspace,
                    ProjectId = ScopeIds.DefaultProject
                });

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        meter.Verify(
            usage =>
                usage.RecordAsync(It.IsAny<UsageEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task InvokeAsync_when_tenant_id_empty_does_not_record()
    {
        Mock<IScopeContextProvider> scopes = new();
        Mock<IUsageMeteringService> meter = new();
        ApiRequestMeteringMiddleware middleware = new(
            scopes.Object,
            meter.Object,
            CreateMeteringOptions(true).Object,
            NullLogger<ApiRequestMeteringMiddleware>.Instance);

        DefaultHttpContext context = CreateContext("/v1/runs");
        scopes.Setup(scopeProvider => scopeProvider.GetCurrentScope())
            .Returns(
                new ScopeContext
                {
                    TenantId = Guid.Empty,
                    WorkspaceId = ScopeIds.DefaultWorkspace,
                    ProjectId = ScopeIds.DefaultProject
                });

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        meter.Verify(
            usage =>
                usage.RecordAsync(It.IsAny<UsageEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task InvokeAsync_when_eligible_records_api_request_meter_event()
    {
        Mock<IScopeContextProvider> scopes = new();
        Mock<IUsageMeteringService> meter = new();
        meter.Setup(usage =>
                usage.RecordAsync(It.IsAny<UsageEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ApiRequestMeteringMiddleware middleware = new(
            scopes.Object,
            meter.Object,
            CreateMeteringOptions(true).Object,
            NullLogger<ApiRequestMeteringMiddleware>.Instance);

        DefaultHttpContext context = CreateContext("/v1/architecture/runs");
        ScopeContext scope = new()
        {
            TenantId = NonEmptyTenant, WorkspaceId = ScopeIds.DefaultWorkspace, ProjectId = ScopeIds.DefaultProject
        };
        scopes.Setup(scopeProvider => scopeProvider.GetCurrentScope()).Returns(scope);

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        meter.Verify(
            usage =>
                usage.RecordAsync(
                    It.Is<UsageEvent>(evt =>
                        evt.Kind == UsageMeterKind.ApiRequest
                        && evt.TenantId == NonEmptyTenant
                        && evt.WorkspaceId == ScopeIds.DefaultWorkspace
                        && evt.ProjectId == ScopeIds.DefaultProject
                        && evt.Quantity == 1L
                        && evt.CorrelationId == "trace-1"),
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [SkippableFact]
    public async Task InvokeAsync_when_record_throws_does_not_propagate_after_successful_next()
    {
        Mock<IScopeContextProvider> scopes = new();
        Mock<IUsageMeteringService> meter = new();
        meter.Setup(usage =>
                usage.RecordAsync(It.IsAny<UsageEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("meter store offline"));

        ApiRequestMeteringMiddleware middleware = new(
            scopes.Object,
            meter.Object,
            CreateMeteringOptions(true).Object,
            NullLogger<ApiRequestMeteringMiddleware>.Instance);

        DefaultHttpContext context = CreateContext("/v1/runs");
        scopes.Setup(scopeProvider => scopeProvider.GetCurrentScope())
            .Returns(
                new ScopeContext
                {
                    TenantId = NonEmptyTenant,
                    WorkspaceId = ScopeIds.DefaultWorkspace,
                    ProjectId = ScopeIds.DefaultProject
                });

        Func<Task> act = async () =>
            await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        await act.Should().NotThrowAsync();
    }
}
