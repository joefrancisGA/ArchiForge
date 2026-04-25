using System.Security.Claims;

using ArchLucid.Application.Tenancy;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using Moq;

namespace ArchLucid.Api.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class TrialSeatReservationMiddlewareTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static DefaultHttpContext CreateHttpContext(
        PathString path,
        ClaimsPrincipal user,
        Mock<ITenantRepository> tenants)
    {
        DefaultHttpContext http = new()
        {
            Request = { Path = path },
            User = user,
            Response = { Body = new MemoryStream() }
        };

        ServiceCollection services = [];
        services.AddSingleton<IHttpContextAccessor>(_ => new HttpContextAccessor { HttpContext = http });
        services.AddSingleton<IScopeContextProvider, HttpScopeContextProvider>();
        services.AddSingleton(tenants.Object);
        services.AddSingleton<TrialSeatAccountant>();

        http.RequestServices = services.BuildServiceProvider();

        return http;
    }

    private static ClaimsPrincipal AuthenticatedPrincipal(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/health/ready")]
    [InlineData("/version")]
    [InlineData("/openapi/v1.json")]
    [InlineData("/v1/register")]
    [InlineData("/robots.txt")]
    public async Task InvokeAsync_skip_paths_call_next_without_seat_check(string path)
    {
        Mock<ITenantRepository> tenants = new();
        bool nextCalled = false;
        TrialSeatReservationMiddleware middleware = new(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        DefaultHttpContext http = CreateHttpContext(
            path,
            AuthenticatedPrincipal(new Claim("sub", "x"), new Claim("tenant_id", TenantId.ToString("D"))),
            tenants);

        await middleware.InvokeAsync(http);

        nextCalled.Should().BeTrue();
        tenants.Verify(
            repository =>
                repository.TryClaimTrialSeatAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_anonymous_user_calls_next_without_seat_check()
    {
        Mock<ITenantRepository> tenants = new();
        bool nextCalled = false;
        TrialSeatReservationMiddleware middleware = new(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        DefaultHttpContext http = CreateHttpContext("/v1/runs", new ClaimsPrincipal(), tenants);

        await middleware.InvokeAsync(http);

        nextCalled.Should().BeTrue();
        tenants.Verify(
            repository =>
                repository.TryClaimTrialSeatAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_authenticated_without_principal_key_calls_next_without_seat_check()
    {
        Mock<ITenantRepository> tenants = new();
        bool nextCalled = false;
        TrialSeatReservationMiddleware middleware = new(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        DefaultHttpContext http = CreateHttpContext(
            "/v1/runs",
            AuthenticatedPrincipal(new Claim("tenant_id", TenantId.ToString("D"))),
            tenants);

        await middleware.InvokeAsync(http);

        nextCalled.Should().BeTrue();
        tenants.Verify(
            repository =>
                repository.TryClaimTrialSeatAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_reserves_seat_using_sub_when_present()
    {
        Mock<ITenantRepository> tenants = new();
        tenants.Setup(repository =>
                repository.TryClaimTrialSeatAsync(TenantId, "user-sub", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        bool nextCalled = false;
        TrialSeatReservationMiddleware middleware = new(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        DefaultHttpContext http = CreateHttpContext(
            "/v1/runs",
            AuthenticatedPrincipal(
                new Claim("sub", "user-sub"),
                new Claim("tenant_id", TenantId.ToString("D"))),
            tenants);

        await middleware.InvokeAsync(http);

        nextCalled.Should().BeTrue();
        tenants.Verify(
            repository => repository.TryClaimTrialSeatAsync(TenantId, "user-sub", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_reserves_seat_using_object_identifier_when_sub_absent()
    {
        Mock<ITenantRepository> tenants = new();
        tenants.Setup(repository =>
                repository.TryClaimTrialSeatAsync(TenantId, "oid-value", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        bool nextCalled = false;
        TrialSeatReservationMiddleware middleware = new(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        DefaultHttpContext http = CreateHttpContext(
            "/v1/runs",
            AuthenticatedPrincipal(
                new Claim(
                    "http://schemas.microsoft.com/identity/claims/objectidentifier",
                    "oid-value"),
                new Claim("tenant_id", TenantId.ToString("D"))),
            tenants);

        await middleware.InvokeAsync(http);

        nextCalled.Should().BeTrue();
        tenants.Verify(
            repository => repository.TryClaimTrialSeatAsync(TenantId, "oid-value", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_trial_limit_exceeded_writes_402_and_skips_next()
    {
        Mock<ITenantRepository> tenants = new();
        tenants.Setup(repository =>
                repository.TryClaimTrialSeatAsync(TenantId, "user-sub", It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new TrialLimitExceededException(TrialLimitReason.SeatsExceeded, 0));

        bool nextCalled = false;
        TrialSeatReservationMiddleware middleware = new(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        DefaultHttpContext http = CreateHttpContext(
            "/v1/runs",
            AuthenticatedPrincipal(
                new Claim("sub", "user-sub"),
                new Claim("tenant_id", TenantId.ToString("D"))),
            tenants);

        await middleware.InvokeAsync(http);

        nextCalled.Should().BeFalse();
        http.Response.StatusCode.Should().Be(StatusCodes.Status402PaymentRequired);
    }
}
