using ArchiForge.Application;

using FluentAssertions;

using Microsoft.AspNetCore.Http;

using Moq;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Unit")]
public sealed class ActorContextTests
{
    [Fact]
    public void GetActor_WhenIdentityNamePresent_ReturnsTrimmedName()
    {
        Mock<IHttpContextAccessor> accessor = new();
        DefaultHttpContext httpContext = new();
        httpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "  domain\\alice  ")],
                authenticationType: "test"));
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
        DefaultHttpContext httpContext = new();
        httpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(authenticationType: "test"));
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        ActorContext sut = new(accessor.Object);

        string actor = sut.GetActor();

        actor.Should().Be("api-user");
        actor.Should().NotBeNullOrWhiteSpace();
    }
}
