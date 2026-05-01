using System.Net;

using ArchLucid.Decisioning.Advisory.Delivery;
using ArchLucid.Host.Core.Services.Delivery;

using FluentAssertions;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Api.Tests;

/// <summary>
///    Verifies outbound webhook telemetry scopes in <see cref="HttpWebhookPoster"/> (privacy-safe; no paths or payloads).
/// </summary>
[Trait("Category", "Unit")]
public sealed class HttpWebhookPosterInstrumentationTests
{
    [SkippableFact]
    public async Task HttpWebhookPoster_logs_and_scopes_correct_fields_OnSuccess()
    {
        TelemetryLogger<HttpWebhookPoster> telemetryLogger = new();

        RecordingHttpStubHandler stub = new(() => new HttpResponseMessage(HttpStatusCode.OK));

        HttpWebhookPoster poster = new(telemetryLogger, new SingletonWebhookClientFactory(stub));

        Guid tenantId = Guid.Parse("a1df5c94-81c3-4125-9035-93c4c5c5c511");

        await poster.PostJsonAsync(
            "https://hooks.partner.test/with/secret/token",
            new { Hello = "world" },
            CancellationToken.None,
            new WebhookPostOptions { EventType = "archlucid.digest.sent", TenantId = tenantId, });

        telemetryLogger.ScopeStates.Should().ContainSingle();

        IReadOnlyDictionary<string, object?> scope = telemetryLogger.ScopeStates[0];

        scope.Should().ContainKey("archlucid.webhook.status_code");

        ScopeInt(scope["archlucid.webhook.status_code"]).Should().Be((int)HttpStatusCode.OK);

        ScopeLong(scope["archlucid.webhook.duration_ms"]).Should().BeGreaterOrEqualTo(0);

        scope["archlucid.webhook.target_authority"].Should().Be("https://hooks.partner.test");
        scope["archlucid.webhook.event_type"].Should().Be("archlucid.digest.sent");
        scope["archlucid.webhook.tenant_id"].Should().Be(tenantId);
        scope["archlucid.webhook.succeeded"].Should().Be(true);

        telemetryLogger.LogWrites.Should().ContainSingle(static w =>
            w.Level == LogLevel.Information
            && w.Message.StartsWith("Webhook outbound HTTP POST attempt completed.", StringComparison.Ordinal));

        telemetryLogger.MessagesJoined().Should().NotContain("with/secret");

        telemetryLogger.MessagesJoined().Should().NotContain("Hello");

        telemetryLogger.MessagesJoined().Should().NotContain("world");
        stub.SendCount.Should().Be(1);
    }

    [SkippableFact]
    public async Task HttpWebhookPoster_logs_correct_fields_when_response_is_terminal_non_success()
    {
        TelemetryLogger<HttpWebhookPoster> telemetryLogger = new();

        RecordingHttpStubHandler stub =
            new(() => new HttpResponseMessage(HttpStatusCode.BadRequest));

        HttpWebhookPoster poster = new(telemetryLogger, new SingletonWebhookClientFactory(stub));

        Func<Task> act = async () => await poster.PostJsonAsync(
            "https://hooks.partner.test/fail/route",
            new { Sensitive = true },
            CancellationToken.None,
            new WebhookPostOptions { EventType = "alert.webhook.notify", TenantId = Guid.Empty });

        await act.Should().ThrowExactlyAsync<HttpRequestException>();

        telemetryLogger.ScopeStates.Should().ContainSingle();

        IReadOnlyDictionary<string, object?> scope = telemetryLogger.ScopeStates[0];

        ScopeInt(scope["archlucid.webhook.status_code"]).Should().Be((int)HttpStatusCode.BadRequest);

        ScopeLong(scope["archlucid.webhook.duration_ms"]).Should().BeGreaterOrEqualTo(0);
        scope["archlucid.webhook.target_authority"].Should().Be("https://hooks.partner.test");
        scope["archlucid.webhook.event_type"].Should().Be("alert.webhook.notify");
        scope["archlucid.webhook.tenant_id"].Should().Be(Guid.Empty);
        scope["archlucid.webhook.succeeded"].Should().Be(false);

        telemetryLogger.LogWrites.Should().ContainSingle(static w =>
            w.Message.StartsWith("Webhook outbound HTTP POST attempt completed.", StringComparison.Ordinal)
            && w.Level == LogLevel.Warning);

        telemetryLogger.MessagesJoined().Should().NotContain("/fail/route");

        telemetryLogger.MessagesJoined().Should().NotContain("Sensitive");

        stub.SendCount.Should().Be(1);
    }

    private static int ScopeInt(object? boxed)
    {
        boxed.Should().NotBeNull();
        boxed.Should().BeAssignableTo<ValueType>();

        return Convert.ToInt32(boxed, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static long ScopeLong(object? boxed)
    {
        boxed.Should().NotBeNull();
        boxed.Should().BeAssignableTo<ValueType>();

        return Convert.ToInt64(boxed, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <remarks>Passes the injected <see cref="HttpMessageHandler"/> to every ArchLucid webhooks HttpClient.</remarks>
    private sealed class SingletonWebhookClientFactory(HttpMessageHandler handler)
        : IHttpClientFactory,
            IDisposable
    {
        private readonly HttpMessageHandler _handler =
            handler ?? throw new ArgumentNullException(nameof(handler));

        private HttpClient? _shared;

        /// <inheritdoc />
        public HttpClient CreateClient(string name)
        {
            _ = string.IsNullOrWhiteSpace(name) ? string.Empty : name;

            HttpClient pooled = LazilyCreateSingleton();

            return pooled;
        }

        public void Dispose()
        {
            _shared?.Dispose();
            _handler.Dispose();
        }

        private HttpClient LazilyCreateSingleton()
        {
            if (_shared is not null)
                return _shared;

            HttpClient created = new(_handler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(30), };

            _shared = created;

            return created;
        }
    }

    private sealed class RecordingHttpStubHandler(Func<HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> _factory =
            responseFactory ?? throw new ArgumentNullException(nameof(responseFactory));

        public int SendCount
        {
            get;
            private set;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.RequestUri);

            SendCount++;

            return Task.FromResult(_factory());
        }
    }

    private sealed class TelemetryLogger<T> : ILogger<T>, IDisposable
    {
        public List<IReadOnlyDictionary<string, object?>> ScopeStates
        {
            get;
        } = [];

        public List<(LogLevel Level, string Message)> LogWrites
        {
            get;
        } = [];

        IDisposable ILogger.BeginScope<TState>(TState state)
        {
            if (state is IEnumerable<KeyValuePair<string, object?>> pairs)

                ScopeStates.Add(CapturePairs(pairs));

            return TelemetryScopeDisposable.Instance;
        }

        bool ILogger.IsEnabled(LogLevel logLevel)
        {
            return logLevel is not LogLevel.None;
        }

        void ILogger.Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (formatter is null)
                return;

            string formatted = formatter(state, exception);

            LogWrites.Add((logLevel, formatted));
        }

        public string MessagesJoined() =>
            string.Join("|", LogWrites.ConvertAll(static tuple => $"{tuple.Level}:{tuple.Message}"));

        public void Dispose()
        {
        }

        private static Dictionary<string, object?> CapturePairs(IEnumerable<KeyValuePair<string, object?>> source)
        {
            Dictionary<string, object?> cloned = [];

            foreach (KeyValuePair<string, object?> pair in source)

                cloned[pair.Key] = pair.Value;

            return cloned;
        }
    }

    private sealed class TelemetryScopeDisposable : IDisposable
    {
        public static TelemetryScopeDisposable Instance
        {
            get;
        } = new();

        void IDisposable.Dispose()
        {
        }
    }
}
