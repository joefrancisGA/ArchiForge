namespace ArchLucid.Api.Routing;

/// <summary>Relative URL segments (no leading slash) for versioned operator/governance APIs — use with <c>"/" + ApiV1Routes.X</c> in tests and clients.</summary>
public static class ApiV1Routes
{
    public const string PolicyPacks = "v1/policy-packs";
    public const string GovernanceResolution = "v1/governance-resolution";
    public const string AlertRules = "v1/alert-rules";
    public const string Alerts = "v1/alerts";
    public const string CompositeAlertRules = "v1/composite-alert-rules";
    public const string AlertSimulation = "v1/alert-simulation";
    public const string AlertTuning = "v1/alert-tuning";
    public const string AlertRoutingSubscriptions = "v1/alert-routing-subscriptions";
    public const string DigestSubscriptions = "v1/digest-subscriptions";

    public const string TenantExecDigestPreferences = "v1/tenant/exec-digest-preferences";

    public const string Evolution = "v1/evolution";
}
