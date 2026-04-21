using System.Text.Json;

using ArchLucid.Contracts.Abstractions.Integrations;
using ArchLucid.Core.Integration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Integrations.AzureDevOps;

/// <summary>
/// Consumes <see cref="IntegrationEventTypes.AuthorityRunCompletedV1"/> and optionally decorates a configured Azure DevOps PR.
/// </summary>
public sealed class AuthorityRunCompletedAzureDevOpsIntegrationEventHandler(
    IAzureDevOpsPullRequestDecorator decorator,
    IOptions<AzureDevOpsIntegrationOptions> options,
    ILogger<AuthorityRunCompletedAzureDevOpsIntegrationEventHandler> logger) : IIntegrationEventHandler
{
    private readonly IAzureDevOpsPullRequestDecorator _decorator =
        decorator ?? throw new ArgumentNullException(nameof(decorator));

    private readonly IOptions<AzureDevOpsIntegrationOptions> _options =
        options ?? throw new ArgumentNullException(nameof(options));

    private readonly ILogger<AuthorityRunCompletedAzureDevOpsIntegrationEventHandler> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public string EventType => IntegrationEventTypes.AuthorityRunCompletedV1;

    /// <inheritdoc />
    public async Task HandleAsync(ReadOnlyMemory<byte> utf8JsonPayload, CancellationToken cancellationToken)
    {
        AzureDevOpsIntegrationOptions o = _options.Value;

        if (!o.Enabled)
            return;


        if (o.RepositoryId == Guid.Empty || o.PullRequestId <= 0)
        {
            if (_logger.IsEnabled(LogLevel.Debug))

                _logger.LogDebug("Azure DevOps PR decoration skipped: RepositoryId or PullRequestId not set.");

            return;
        }

        AuthorityRunCompletedPayload? payload;

        try
        {
            payload = JsonSerializer.Deserialize<AuthorityRunCompletedPayload>(
                utf8JsonPayload.Span,
                AuthorityRunCompletedPayloadJson.Options);
        }
        catch (JsonException ex)
        {
            throw new FormatException("Authority run completed payload was not valid JSON.", ex);
        }

        if (payload is null)
            throw new FormatException("Authority run completed payload deserialized to null.");


        AzureDevOpsPullRequestTarget target = new(o.RepositoryId, o.PullRequestId);

        await _decorator
            .PostManifestDeltaAsync(payload.ManifestId, payload.RunId, target, cancellationToken)
            .ConfigureAwait(false);
    }

    private sealed record AuthorityRunCompletedPayload(
        int SchemaVersion,
        Guid RunId,
        Guid ManifestId,
        Guid TenantId,
        Guid WorkspaceId,
        Guid ProjectId);
}

internal static class AuthorityRunCompletedPayloadJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };
}
