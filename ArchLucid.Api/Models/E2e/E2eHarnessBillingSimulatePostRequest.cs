namespace ArchLucid.Api.Models.E2e;

/// <summary>Body for <c>POST /v1/e2e/billing/simulate-subscription-activated</c> (harness only).</summary>
// ReSharper disable once InconsistentNaming
public sealed class E2eHarnessBillingSimulatePostRequest
{
    public Guid TenantId
    {
        get; init;
    }

    public Guid WorkspaceId
    {
        get; init;
    }

    public Guid ProjectId
    {
        get; init;
    }

    /// <summary>Must match the session id returned from <c>POST /v1/tenant/billing/checkout</c> for Noop (e.g. <c>noop_sess_*</c>).</summary>
    public string ProviderSubscriptionId { get; init; } = string.Empty;

    /// <summary><c>Team</c>, <c>Pro</c>, or <c>Enterprise</c> (default <c>Team</c>).</summary>
    public string CheckoutTier { get; init; } = "Team";

    /// <summary>Billing provider name (default <c>Noop</c> for CI).</summary>
    public string Provider { get; init; } = "Noop";
}
