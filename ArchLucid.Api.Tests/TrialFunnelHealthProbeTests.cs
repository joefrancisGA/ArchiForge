using System.Net;

using ArchLucid.Api.Hosting;

using FluentAssertions;

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Moq;

namespace ArchLucid.Api.Tests;

[Trait("Suite", "Core")]
public sealed class TrialFunnelHealthProbeTests
{
    [Fact]
    public void TryMapToLoopbackBase_maps_wildcard_ipv4_to_127_0_0_1()
    {
        string? u = TrialFunnelHealthProbe.TryMapToLoopbackBase("http://0.0.0.0:5000");
        u.Should().Be("http://127.0.0.1:5000");
    }

    [Fact]
    public void TryMapToLoopbackBase_preserves_explicit_localhost()
    {
        string? u = TrialFunnelHealthProbe.TryMapToLoopbackBase("http://127.0.0.1:9");
        u.Should().Be("http://127.0.0.1:9");
    }

    [Fact]
    public void TryMapToLoopbackBase_maps_bracket_any_ipv6_to_127_0_0_1()
    {
        string? u = TrialFunnelHealthProbe.TryMapToLoopbackBase("http://[::]:5000");
        u.Should().Be("http://127.0.0.1:5000");
    }

    [Fact]
    public async Task RunSingleProbeForTestsAsync_returns_true_on_200()
    {
        using TestHttpMessageHandler handler = new(
            _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
        using HttpClient http = new(handler);
        Mock<IHttpClientFactory> factory = new();
        _ = factory.Setup(f => f.CreateClient(TrialFunnelHealthProbe.HttpClientName)).Returns(http);

        IFeatureCollection features = new FeatureCollection();
        ServerAddressesFeature addresses = new();
        addresses.Addresses.Add("http://0.0.0.0:0");
        features.Set<IServerAddressesFeature>(addresses);
        Mock<IServer> server = new();
        _ = server.Setup(s => s.Features).Returns(features);

        TrialFunnelHealthProbe probe = new(
            factory.Object,
            new ImmediateApplicationLifetime(),
            new LoggerFactory().CreateLogger<TrialFunnelHealthProbe>(),
            server.Object);

        bool ok = await probe.RunSingleProbeForTestsAsync();
        ok.Should().BeTrue();
    }

    [Fact]
    public async Task RunSingleProbeForTestsAsync_returns_false_on_non_200()
    {
        using TestHttpMessageHandler handler = new(
            _ => new HttpResponseMessage(HttpStatusCode.NotFound));
        using HttpClient http = new(handler);
        Mock<IHttpClientFactory> factory = new();
        _ = factory.Setup(f => f.CreateClient(TrialFunnelHealthProbe.HttpClientName)).Returns(http);

        IFeatureCollection features = new FeatureCollection();
        ServerAddressesFeature addresses = new();
        addresses.Addresses.Add("http://127.0.0.1:0");
        features.Set<IServerAddressesFeature>(addresses);
        Mock<IServer> server = new();
        _ = server.Setup(s => s.Features).Returns(features);

        TrialFunnelHealthProbe probe = new(
            factory.Object,
            new ImmediateApplicationLifetime(),
            new LoggerFactory().CreateLogger<TrialFunnelHealthProbe>(),
            server.Object);

        bool ok = await probe.RunSingleProbeForTestsAsync();
        ok.Should().BeFalse();
    }

    private sealed class ImmediateApplicationLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted { get; } = new CancellationToken(canceled: true);
        public CancellationToken ApplicationStopped { get; } = CancellationToken.None;
        public CancellationToken ApplicationStopping { get; } = CancellationToken.None;

        public void StopApplication() { }
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _onSend;

        public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> onSend) => _onSend = onSend;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(_onSend(request));
    }
}
