using ArchLucid.Core.Configuration;
using ArchLucid.Core.Notifications.Email;

using Microsoft.Extensions.Options;

namespace ArchLucid.Persistence.Notifications.Email;

public sealed class AzureCommunicationServicesEmailProvider(
    IOptionsMonitor<EmailNotificationOptions> optionsMonitor,
    IAzureCommunicationEmailApi acsApi) : IEmailProvider
{
    private readonly IAzureCommunicationEmailApi _acsApi = acsApi ?? throw new ArgumentNullException(nameof(acsApi));

    private readonly IOptionsMonitor<EmailNotificationOptions> _optionsMonitor =
        optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));

    /// <inheritdoc />
    public string ProviderName => EmailProviderNames.AzureCommunicationServices;

    /// <inheritdoc />
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        EmailNotificationOptions options = _optionsMonitor.CurrentValue;

        if (string.IsNullOrWhiteSpace(options.AzureCommunicationServicesEndpoint))

            throw new InvalidOperationException(
                "Email:AzureCommunicationServicesEndpoint is required when Email:Provider is AzureCommunicationServices.");


        if (string.IsNullOrWhiteSpace(options.FromAddress))

            throw new InvalidOperationException(
                "Email:FromAddress is required when Email:Provider is AzureCommunicationServices.");


        string messageId = await _acsApi.SendAsync(
                options.AzureCommunicationServicesEndpoint.Trim(),
                options.AzureManagedIdentityClientId,
                options.FromAddress.Trim(),
                message.To.Trim(),
                message.Subject,
                message.TextBody,
                message.HtmlBody,
                cancellationToken)
            .ConfigureAwait(false);

        _ = messageId;
    }
}
