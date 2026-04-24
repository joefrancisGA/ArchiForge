using ArchLucid.Api.Controllers.Admin;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class DocsControllerTests
{
    [Fact]
    public void ReplayRecipes_returns_html_linking_scalar_and_replay_endpoints()
    {
        DocsController sut = new();
        DefaultHttpContext http = new()
        {
            Request = { Scheme = "https", Host = new HostString("api.example.com"), PathBase = PathString.Empty }
        };
        sut.ControllerContext = new ControllerContext { HttpContext = http };

        IActionResult result = sut.ReplayRecipes();

        ContentResult content = result.Should().BeOfType<ContentResult>().Subject;
        content.ContentType.Should().Be("text/html");
        content.Content.Should().Contain("ArchLucid comparison replay recipes");
        content.Content.Should().Contain("https://api.example.com/scalar/v1");
        content.Content.Should().Contain("/v1/architecture/comparisons");
    }
}
