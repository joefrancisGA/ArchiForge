namespace ArchLucid.Core.Billing;

/// <summary>One append-only row from <c>dbo.BillingSubscriptionStateHistory</c> (subscription mutation audit).</summary>
public sealed class BillingSubscriptionStateHistoryEntry
{
    public Guid HistoryId
    {
        get;
        set;
    }

    public Guid TenantId
    {
        get;
        set;
    }

    public Guid WorkspaceId
    {
        get;
        set;
    }

    public Guid ProjectId
    {
        get;
        set;
    }

    public DateTimeOffset RecordedUtc
    {
        get;
        set;
    }

    public string ChangeKind
    {
        get;
        set;
    } = string.Empty;

    public string? PrevStatus
    {
        get;
        set;
    }

    public string? NewStatus
    {
        get;
        set;
    }

    public string? PrevTier
    {
        get;
        set;
    }

    public string? NewTier
    {
        get;
        set;
    }

    public int? PrevSeatsPurchased
    {
        get;
        set;
    }

    public int? NewSeatsPurchased
    {
        get;
        set;
    }

    public int? PrevWorkspacesPurchased
    {
        get;
        set;
    }

    public int? NewWorkspacesPurchased
    {
        get;
        set;
    }

    public string? PrevProvider
    {
        get;
        set;
    }

    public string? NewProvider
    {
        get;
        set;
    }

    public string? PrevProviderSubscriptionId
    {
        get;
        set;
    }

    public string? NewProviderSubscriptionId
    {
        get;
        set;
    }
}
