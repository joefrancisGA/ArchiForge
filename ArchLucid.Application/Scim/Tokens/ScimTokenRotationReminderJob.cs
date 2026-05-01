using System.Text.Json;

using ArchLucid.Core.AdminNotifications;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Scim;
using ArchLucid.Core.Scim.Models;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Scim.Tokens;

/// <summary>
///     Daily posture scan that warns when active SCIM tokens exceed
///     <see cref="ScimOptions.TokenRotationReminderDays" />.
/// </summary>
public sealed class ScimTokenRotationReminderJob(
    IServiceScopeFactory scopeFactory,
    IOptions<ScimOptions> options,
    ILogger<ScimTokenRotationReminderJob> logger) : BackgroundService
{
    private readonly ILogger<ScimTokenRotationReminderJob> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IOptions<ScimOptions> _options = options ?? throw new ArgumentNullException(nameof(options));

    private readonly IServiceScopeFactory _scopeFactory =
        scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan cadence = TimeSpan.FromHours(24);

        try
        {
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunReminderScanAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "SCIM token rotation reminder scan failed.");
            }

            try
            {
                await Task.Delay(cadence, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunReminderScanAsync(CancellationToken ct)
    {
        int reminderDays = _options.Value.TokenRotationReminderDays;

        if (reminderDays <= 0)
            return;

        DateTimeOffset cutoffUtc = DateTimeOffset.UtcNow.AddDays(-reminderDays);

        using IServiceScope scope = _scopeFactory.CreateScope();
        IScimTenantTokenRepository tokens = scope.ServiceProvider.GetRequiredService<IScimTenantTokenRepository>();
        IAdminNotificationsRepository notices =
            scope.ServiceProvider.GetRequiredService<IAdminNotificationsRepository>();

        IReadOnlyList<ScimTokenRotationCandidate> due =
            await tokens.ListActiveCreatedOnOrBeforeAsync(cutoffUtc, ct).ConfigureAwait(false);

        foreach (ScimTokenRotationCandidate row in due)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning(
                    "archlucid.scim.token.rotation_due tenantId={TenantId} tokenId={TokenId} createdUtc={CreatedUtc:o}",
                    row.TenantId,
                    row.Id,
                    row.CreatedUtc);

            string dataJson =
                JsonSerializer.Serialize(
                    new { tenantId = row.TenantId, tokenId = row.Id, createdUtc = row.CreatedUtc });

            await notices
                .InsertAsync(
                    "scim_token_rotation_due",
                    $"SCIM bearer token {row.Id:D} for tenant {row.TenantId:D} is older than the configured rotation reminder ({reminderDays} days). Rotate or revoke in admin.",
                    dataJson,
                    ct)
                .ConfigureAwait(false);
        }
    }
}
