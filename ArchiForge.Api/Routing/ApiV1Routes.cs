namespace ArchiForge.Api.Routing;

/// <summary>Relative URL segments (no leading slash) for versioned operator/governance APIs — use with <c>"/" + ApiV1Routes.X</c> in tests and clients.</summary>
public static class ApiV1Routes
{
    public const string PolicyPacks = "v1/policy-packs";
    public const string AlertRules = "v1/alert-rules";
    public const string Alerts = "v1/alerts";
}
