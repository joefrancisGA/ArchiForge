using System.Net;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Ensures <c>Cors:AllowedOrigins</c> does not reflect arbitrary browser <c>Origin</c> values (defense in depth with auth).
/// </summary>
[Trait("Category", "Integration")]
public sealed class CorsPolicyIntegrationTests(CorsTrustedOriginApiFactory factory)
    : IClassFixture<CorsTrustedOriginApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task HealthCheck_IncludesW3CTraceResponseHeaders_OnSuccess()
    {
        HttpResponseMessage response = await _client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("traceparent", out IEnumerable<string>? tp).Should().BeTrue();
        tp!.Should().ContainSingle();
        tp.First().Should().MatchRegex("^00-[0-9a-fA-F]{32}-[0-9a-fA-F]{16}-[0-9a-fA-F]{2}$");

        response.Headers.TryGetValues("X-Trace-Id", out IEnumerable<string>? xt).Should().BeTrue();
        xt!.Should().ContainSingle();
        xt.First().Should().MatchRegex("^[0-9a-fA-F]{32}$");
    }

    [Fact]
    public async Task HealthCheck_DoesNotEmitAllowOrigin_ForDisallowedOrigin()
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "/health/ready");
        request.Headers.TryAddWithoutValidation("Origin", "https://malicious.example");

        HttpResponseMessage response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Access-Control-Allow-Origin", out IEnumerable<string>? _).Should().BeFalse();
    }

    [Fact]
    public async Task HealthCheck_EmitsAllowOrigin_ForConfiguredOrigin()
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "/health/ready");
        request.Headers.TryAddWithoutValidation("Origin", "https://trusted.app.example");

        HttpResponseMessage response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string? acao = response.Headers.TryGetValues("Access-Control-Allow-Origin", out IEnumerable<string>? v)
            ? v.FirstOrDefault()
            : null;
        acao.Should().Be("https://trusted.app.example");
    }

    [Fact]
    public async Task PreflightOptions_FromTrustedOrigin_WithPost_ReflectsAllowedMethods()
    {
        using HttpRequestMessage request = new(HttpMethod.Options, "/health/ready");
        request.Headers.TryAddWithoutValidation("Origin", "https://trusted.app.example");
        request.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "POST");
        request.Headers.TryAddWithoutValidation("Access-Control-Request-Headers", "content-type");

        HttpResponseMessage response = await _client.SendAsync(request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);
        response.Headers.TryGetValues("Access-Control-Allow-Origin", out IEnumerable<string>? acao).Should().BeTrue();
        acao!.Should().ContainSingle().Which.Should().Be("https://trusted.app.example");

        response.Headers.TryGetValues("Access-Control-Allow-Methods", out IEnumerable<string>? methods).Should().BeTrue();
        string joined = string.Join(", ", methods!);
        joined.Contains("POST", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
    }

    [Fact]
    public async Task PreflightOptions_FromTrustedOrigin_WithDisallowedRequestHeader_IsRejected()
    {
        using HttpRequestMessage request = new(HttpMethod.Options, "/health/ready");
        request.Headers.TryAddWithoutValidation("Origin", "https://trusted.app.example");
        request.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "POST");
        request.Headers.TryAddWithoutValidation("Access-Control-Request-Headers", "X-Custom-Header");

        HttpResponseMessage response = await _client.SendAsync(request);

        bool allowsCustom =
            response.Headers.TryGetValues("Access-Control-Allow-Headers", out IEnumerable<string>? allowHeaders)
            && HeaderNamesContainsToken(allowHeaders, "X-Custom-Header");

        allowsCustom.Should().BeFalse(
            because: "requested header is not in the explicit Cors:AllowedHeaders allow-list");
    }

    private static bool HeaderNamesContainsToken(IEnumerable<string> headerValues, string token)
    {
        foreach (string part in headerValues)
        {
            foreach (string name in part.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (name.Equals(token, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }
}
