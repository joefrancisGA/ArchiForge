using System.Net;
using System.Net.Http.Headers;

using ArchLucid.Core.Diagnostics;

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace ArchLucid.Api.Hosting;

/// <summary>
///     When <c>Demo:Enabled=true</c>, periodically GETs <c>/v1/demo/preview</c> (same precomposed bundle the trial
///     funnel and marketing use) to verify the read path is healthy. Does not use Stripe or production-only keys.
/// </summary>
public sealed class TrialFunnelHealthProbe : BackgroundService
{
    public const string HttpClientName = "TrialFunnelHealthProbe";
    public const string DemoPreviewRelativePath = "/v1/demo/preview";
    private static readonly TimeSpan ProbeInterval = TimeSpan.FromMinutes(5);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<TrialFunnelHealthProbe> _logger;
    private readonly IServer _server;

    public TrialFunnelHealthProbe(
        IHttpClientFactory httpClientFactory,
        IHostApplicationLifetime lifetime,
        ILogger<TrialFunnelHealthProbe> logger,
        IServer server)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _server = server ?? throw new ArgumentNullException(nameof(server));
    }

    /// <summary>Builds a loopback base URL from a Kestrel listen address (internal for unit tests).</summary>
    internal static string? TryMapToLoopbackBase(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return null;

        if (!Uri.TryCreate(address.Trim(), UriKind.Absolute, out Uri? u))
            return null;

        UriBuilder b = new(u);
        string h = b.Host;
        if (h is "0.0.0.0" or "" or "*" or "+")
        {
            b.Host = "127.0.0.1";
            return b.Uri.GetLeftPart(UriPartial.Authority);
        }

        if (h is "[::]" or "[::0]" or "::")
        {
            b.Host = "127.0.0.1";
            return b.Uri.GetLeftPart(UriPartial.Authority);
        }

        return b.Uri.GetLeftPart(UriPartial.Authority);
    }

    internal string ResolveBaseUrl()
    {
        IServerAddressesFeature? addrs = _server.Features.Get<IServerAddressesFeature>();
        if (addrs?.Addresses is { Count: > 0 } set)
        {
            foreach (string? a in set)
            {
                string? mapped = TryMapToLoopbackBase(a);
                if (!string.IsNullOrEmpty(mapped))
                    return mapped;
            }
        }

        if (_logger.IsEnabled(LogLevel.Warning))
            _logger.LogWarning("Trial funnel health probe: no usable Kestrel address; defaulting to http://127.0.0.1:5000.");

        return "http://127.0.0.1:5000";
    }

    /// <summary>Single GET for tests (Core suite) — no periodic loop.</summary>
    internal async Task<bool> RunSingleProbeForTestsAsync(CancellationToken cancellationToken = default) =>
        await RunProbeRequestAsync(ResolveBaseUrl(), cancellationToken).ConfigureAwait(false);

    private async Task<bool> RunProbeRequestAsync(string baseUrl, CancellationToken cancellationToken)
    {
        HttpClient client = _httpClientFactory.CreateClient(HttpClientName);
        Uri requestUri = new(new Uri(baseUrl, UriKind.Absolute), DemoPreviewRelativePath);
        using HttpResponseMessage response = await client
            .GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            ArchLucidInstrumentation.RecordTrialFunnelHealthProbe("success");
            return true;
        }

        ArchLucidInstrumentation.RecordTrialFunnelHealthProbe("failure");

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Trial funnel health probe: GET {Path} returned {Code}.", requestUri, response.StatusCode);
        return false;
    }

    private static async Task WaitForHostApplicationStartedAsync(
        IHostApplicationLifetime lifetime,
        CancellationToken stoppingToken)
    {
        if (lifetime.ApplicationStarted.IsCancellationRequested) return;
        TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using (lifetime.ApplicationStarted.Register(static state => ((TaskCompletionSource)state!).TrySetResult(), tcs))
        {
            await tcs.Task.WaitAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await WaitForHostApplicationStartedAsync(_lifetime, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        int consecutiveFailures = 0;
        string baseUrl = ResolveBaseUrl();
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Trial funnel health probe: starting against base {BaseUrl} every {Interval} (demo preview {Path}).", baseUrl, ProbeInterval, DemoPreviewRelativePath);

        while (stoppingToken.IsCancellationRequested == false)
        {
            try
            {
                baseUrl = ResolveBaseUrl();
                bool ok = await RunProbeRequestAsync(baseUrl, stoppingToken).ConfigureAwait(false);

                if (ok)
                {
                    if (consecutiveFailures > 0
                        && _logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation(
                            "Trial funnel health probe: recovered after {N} prior consecutive failure(s).", consecutiveFailures);
                    }

                    consecutiveFailures = 0;
                }
                else
                {
                    consecutiveFailures++;

                    if (consecutiveFailures == 3)
                    {
                        if (_logger.IsEnabled(LogLevel.Warning))
                        {
                            _logger.LogWarning(
                                "Trial funnel health probe: {Consecutive} consecutive failures for GET {Path} — verify demo seed and demo preview are healthy.",
                                consecutiveFailures,
                                new Uri(new Uri(baseUrl, UriKind.Absolute), DemoPreviewRelativePath));
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (ex is not TaskCanceledException)
            {
                ArchLucidInstrumentation.RecordTrialFunnelHealthProbe("failure");
                consecutiveFailures++;

                if (consecutiveFailures == 3 && _logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(
                        ex,
                        "Trial funnel health probe: 3 consecutive failures (including network errors) for demo preview.");
                }
                else if (consecutiveFailures < 3 && _logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(ex, "Trial funnel health probe: exception (failure {N}).", consecutiveFailures);
                }
            }

            try
            {
                await Task.Delay(ProbeInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
