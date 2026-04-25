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
}
