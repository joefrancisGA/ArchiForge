using System.Security.Claims;

using ArchLucid.Application.Common;

using FluentAssertions;

using Microsoft.AspNetCore.Http;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Actor Context.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ActorContextTests
{
    [Fact]
    public void GetActor_WhenIdentityNamePresent_ReturnsTrimmedName()
    {
        Mock<IHttpContextAccessor> accessor = new();
        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim(ClaimTypes.Name, "  domain\\alice  ")],
                    "test"))
        };
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        ActorContext sut = new(accessor.Object);

        sut.GetActor().Should().Be("domain\\alice");
    }

    [Fact]
    public void GetActor_WhenNoHttpContext_ReturnsApiUserFallback()
    {
        Mock<IHttpContextAccessor> accessor = new();
        accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        ActorContext sut = new(accessor.Object);

        sut.GetActor().Should().Be("api-user");
    }

    [Fact]
    public void GetActor_WhenIdentityNameEmpty_ReturnsApiUserFallback()
    {
        Mock<IHttpContextAccessor> accessor = new();
        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity("test"))
        };
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        ActorContext sut = new(accessor.Object);

        string actor = sut.GetActor();

        actor.Should().Be("api-user");
        actor.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetActor_WhenOnlyShortJwtNameClaim_ReturnsClaimValue()
    {
        Mock<IHttpContextAccessor> accessor = new();
        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim("name", "  JwtE2eAdmin  ")],
                    "test"))
        };
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        ActorContext sut = new(accessor.Object);

        sut.GetActor().Should().Be("JwtE2eAdmin");
    }

    [Fact]
    public void GetActorId_WhenOidAndTidClaimsPresent_returns_prefixed_tid_oid_key()
    {
        Mock<IHttpContextAccessor> accessor = new();
        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    [
                        new Claim("tid", "  tenant-guid  "),
                        new Claim("oid", " obj-guid "),
                        new Claim("name", "jwt-user"),
                    ],
                    "test"))
        };
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        ActorContext sut = new(accessor.Object);

        sut.GetActorId().Should().Be("jwt:tenant-guid:obj-guid");
    }

    [Fact]
    public void GetActorId_WhenOnlyOid_claim_present_uses_oid_prefix_without_tid()
    {
        Mock<IHttpContextAccessor> accessor = new();
        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim("oid", "only-oid-guid")],
                    "test"))
        };
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        ActorContext sut = new(accessor.Object);

        sut.GetActorId().Should().Be("jwt:only-oid-guid");
    }

    [Fact]
    public void GetActorId_when_oid_claim_absent_returns_GetActor_fallback()
    {
        Mock<IHttpContextAccessor> accessor = new();
        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim("name", "spi-user")],
                    "test")),
        };
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        ActorContext sut = new(accessor.Object);

        sut.GetActor().Should().Be("spi-user");
        sut.GetActorId().Should().Be("spi-user");
    }

    [Fact]
    public void GetActorId_when_long_oid_claim_type_present_matches_short_oid_claim()
    {
        string longOidClaimType =
            "http://schemas.microsoft.com/identity/claims/objectidentifier";

        Mock<IHttpContextAccessor> accessorShort = new();
        DefaultHttpContext httpShort = new()
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    [
                        new Claim("tid", "same-tenant"),
                        new Claim("oid", "same-object"),
                    ],
                    "test")),
        };
        accessorShort.Setup(a => a.HttpContext).Returns(httpShort);

        Mock<IHttpContextAccessor> accessorLong = new();
        DefaultHttpContext httpLong = new()
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    [
                        new Claim("tid", "same-tenant"),
                        new Claim(longOidClaimType, "same-object"),
                    ],
                    "test")),
        };
        accessorLong.Setup(a => a.HttpContext).Returns(httpLong);

        new ActorContext(accessorShort.Object).GetActorId().Should().Be("jwt:same-tenant:same-object");
        new ActorContext(accessorLong.Object).GetActorId().Should().Be("jwt:same-tenant:same-object");
    }

    [Fact]
    public void GetActorId_when_oid_present_differs_from_GetActor_display_when_name_differs_from_oid_semantics()
    {
        // Same Entra oid can arrive with CI SP display vs human UPN-shaped "name"; SoD compares keys, not strings.
        Mock<IHttpContextAccessor> accessor = new();
        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    [
                        new Claim("tid", "t1"),
                        new Claim("oid", "real-oid-guid"),
                        new Claim("name", "friendly-sp-name-only"),
                    ],
                    "test")),
        };
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        ActorContext sut = new(accessor.Object);

        sut.GetActor().Should().Be("friendly-sp-name-only");
        sut.GetActorId().Should().Be("jwt:t1:real-oid-guid");
    }
}
