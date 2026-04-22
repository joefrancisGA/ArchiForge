using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;

namespace ArchLucid.Api.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class FileWithRangeResultTests
{
    [Fact]
    public async Task ExecuteResultAsync_empty_payload_returns_200_with_zero_length()
    {
        DefaultHttpContext http = new()
        {
            Response = { Body = new MemoryStream() }
        };
        ActionContext actionContext = new(http, new RouteData(), new ActionDescriptor());
        FileWithRangeResult sut = new(http.Request, [], "application/octet-stream", "empty.bin");

        await sut.ExecuteResultAsync(actionContext);

        http.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        http.Response.ContentLength.Should().Be(0);
        http.Response.Headers["Accept-Ranges"].ToString().Should().Be("bytes");
    }

    [Fact]
    public async Task ExecuteResultAsync_full_body_when_range_header_absent()
    {
        byte[] payload = [0x01, 0x02, 0x03];
        DefaultHttpContext http = new()
        {
            Response = { Body = new MemoryStream() }
        };
        ActionContext actionContext = new(http, new RouteData(), new ActionDescriptor());
        FileWithRangeResult sut = new(http.Request, payload, "application/octet-stream", "blob.bin");

        await sut.ExecuteResultAsync(actionContext);

        http.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        http.Response.ContentLength.Should().Be(payload.LongLength);
        MemoryStream body = (MemoryStream)http.Response.Body;
        body.ToArray().Should().Equal(payload);
    }

    [Fact]
    public async Task ExecuteResultAsync_partial_content_when_range_valid()
    {
        byte[] payload = [0x10, 0x20, 0x30, 0x40];
        DefaultHttpContext http = new();
        http.Request.Headers.Range = "bytes=1-2";
        http.Response.Body = new MemoryStream();
        ActionContext actionContext = new(http, new RouteData(), new ActionDescriptor());
        FileWithRangeResult sut = new(http.Request, payload, "application/octet-stream", "partial.bin");

        await sut.ExecuteResultAsync(actionContext);

        http.Response.StatusCode.Should().Be(StatusCodes.Status206PartialContent);
        http.Response.ContentLength.Should().Be(2);
        http.Response.Headers.ContentRange.ToString().Should().Be("bytes 1-2/4");
        MemoryStream body = (MemoryStream)http.Response.Body;
        body.ToArray().Should().Equal(0x20, 0x30);
    }
}
