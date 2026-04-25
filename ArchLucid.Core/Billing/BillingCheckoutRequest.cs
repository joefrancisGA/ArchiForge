namespace ArchLucid.Core.Billing;

/// <summary>Input for <see cref="IBillingProvider.CreateCheckoutSessionAsync" />.</summary>
public sealed class BillingCheckoutRequest
{
    public required Guid TenantId
    {
        get;
        init;
    }

    public required Guid WorkspaceId
    {
        get;
        init;
    }

    public required Guid ProjectId
    {
        get;
        init;
    }

    public BillingCheckoutTier TargetTier
    {
        get;
        init;
    }

    public int Seats
    {
        get;
        init;
    }

    public int Workspaces
    {
        get;
        init;
    }

    public string? BillingEmail
    {
        get;
        init;
    }

    public required string ReturnUrl
    {
        get;
        init;
    }

    public required string CancelUrl
    {
        get;
        init;
    }
}
