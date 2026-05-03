namespace ArchLucid.Host.Core.Startup.Validation.Rules;

internal static class ObservabilityRules
{
    public static void CollectOtlp(IConfiguration configuration, List<string> errors)
    {
        bool enabled = configuration.GetValue("Observability:Otlp:Enabled", false);

        if (!enabled)
            return;

        string? endpoint = configuration["Observability:Otlp:Endpoint"]?.Trim();

        if (string.IsNullOrWhiteSpace(endpoint))

            errors.Add(
                "Observability:Otlp:Enabled is true but Observability:Otlp:Endpoint is missing. Set a full OTLP base URL (gRPC or HTTP/protobuf per Observability:Otlp:Protocol).");

        string? protocol = configuration["Observability:Otlp:Protocol"]?.Trim();

        if (string.IsNullOrWhiteSpace(protocol))
            return;

        if (!string.Equals(protocol, "Grpc", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(protocol, "HttpProtobuf", StringComparison.OrdinalIgnoreCase))

            errors.Add(
                "Observability:Otlp:Protocol must be 'Grpc' or 'HttpProtobuf' when set.");
    }

    public static void CollectPrometheus(IConfiguration configuration, List<string> errors)
    {
        bool enabled = configuration.GetValue("Observability:Prometheus:Enabled", false);

        if (!enabled)
            return;

        bool requireAuth = configuration.GetValue("Observability:Prometheus:RequireScrapeAuthentication", true);

        if (!requireAuth)
            return;

        string? user = configuration["Observability:Prometheus:ScrapeUsername"]?.Trim();
        string? password = configuration["Observability:Prometheus:ScrapePassword"];

        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))

            errors.Add(
                "Observability:Prometheus:Enabled is true and RequireScrapeAuthentication defaults to true: configure Observability:Prometheus:ScrapeUsername and ScrapePassword for scrape Basic auth, or set RequireScrapeAuthentication to false (acceptable only on trusted networks).");
    }
}
