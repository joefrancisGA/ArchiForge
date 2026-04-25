using System.Security.Claims;

using ArchLucid.Api.Controllers.Admin;
using ArchLucid.Api.Models;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class AuthDebugControllerTests
{
    [Fact]
    public void Me_returns_ok_with_name_and_claims_from_principal()
    {
        ClaimsIdentity identity = new(
            [new Claim(ClaimTypes.Name, "alice"), new Claim("tid", "tenant-1")],
            "test");
        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(identity)
        };

        AuthDebugController sut = new()
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        IActionResult result = sut.Me();

        OkObjectResult ok = result.Should().BeOfType<OkObjectResult>().Subject;
        CallerIdentityResponse body = ok.Value.Should().BeOfType<CallerIdentityResponse>().Subject;
        body.Name.Should().Be("alice");
        body.Claims.Should().HaveCount(2);
        body.Claims.Should().ContainSingle(c => c.Type == ClaimTypes.Name && c.Value == "alice");
        body.Claims.Should().ContainSingle(c => c.Type == "tid" && c.Value == "tenant-1");
    }

    [Fact]
    public void Me_returns_ok_when_identity_name_is_null()
    {
        ClaimsIdentity identity = new([], "test");
        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(identity)
        };

        AuthDebugController sut = new()
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        IActionResult result = sut.Me();

        OkObjectResult ok = result.Should().BeOfType<OkObjectResult>().Subject;
        CallerIdentityResponse body = ok.Value.Should().BeOfType<CallerIdentityResponse>().Subject;
        body.Name.Should().BeNull();
        body.Claims.Should().BeEmpty();
    }
}
