namespace ArchLucid.Application.Notifications.Email;

/// <summary>JSON body for <see cref="ArchLucid.Core.Integration.IntegrationEventTypes.TrialLifecycleEmailV1" />.</summary>
public sealed class TrialLifecycleEmailIntegrationEnvelope
{
    public int SchemaVersion
    {
        get;
        init;
    } = 1;

    public TrialLifecycleEmailTrigger Trigger
    {
        get;
        init;
    }

    public Guid TenantId
    {
        get;
        init;
    }

    public Guid WorkspaceId
    {
        get;
        init;
    }

    public Guid ProjectId
    {
        get;
        init;
    }

    public Guid? RunId
    {
        get;
        init;
    }

    /// <summary>
    ///     Converted tier label (e.g. Professional) when <see cref="Trigger" /> is
    ///     <see cref="TrialLifecycleEmailTrigger.Converted" />.
    /// </summary>
    public string? TargetTier
    {
        get;
        init;
    }
}
