namespace ArchLucid.Core.Connectors.Publishing;

/// <summary>Outbound publisher port for third-party document / ticket surfaces (Confluence, future ServiceNow/Jira).</summary>
public interface IPublisherConnector
{
    PublishingTargetKind Kind
    {
        get;
    }

    Task<PublishOutcome> PublishAsync(PublishRequest request, CancellationToken cancellationToken);
}
