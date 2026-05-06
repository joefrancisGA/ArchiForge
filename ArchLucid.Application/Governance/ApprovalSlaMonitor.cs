using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Persistence.Data.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace ArchLucid.Application.Governance;
/// <summary>Detects SLA-breached pending approval requests and sends escalation notifications.</summary>
public sealed class ApprovalSlaMonitor
{
    /// <summary>Named <see cref="IHttpClientFactory"/> client for SLA escalation POSTs.</summary>
    public const string SlaEscalationHttpClientName = "SlaEscalation";
    private readonly IGovernanceApprovalRequestRepository _approvalRequestRepository;
    private readonly IAuditService _auditService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApprovalSlaMonitor> _logger;
    private readonly IOptions<PreCommitGovernanceGateOptions> _options;
    public ApprovalSlaMonitor(IGovernanceApprovalRequestRepository approvalRequestRepository, IAuditService auditService, IOptions<PreCommitGovernanceGateOptions> options, IHttpClientFactory httpClientFactory, ILogger<ApprovalSlaMonitor> logger)
    {
        ArgumentNullException.ThrowIfNull(approvalRequestRepository);
        ArgumentNullException.ThrowIfNull(auditService);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _approvalRequestRepository = approvalRequestRepository ?? throw new ArgumentNullException(nameof(approvalRequestRepository));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task CheckAndEscalateAsync(CancellationToken cancellationToken)
    {
        int? slaHours = _options.Value.ApprovalSlaHours;
        if (slaHours is null)
            return;
        IReadOnlyList<GovernanceApprovalRequest> breached = await _approvalRequestRepository.GetPendingSlaBreachedAsync(DateTime.UtcNow, cancellationToken);
        foreach (GovernanceApprovalRequest request in breached)
        {
            try
            {
                await _auditService.LogAsync(new AuditEvent { EventType = AuditEventTypes.GovernanceApprovalSlaBreached, ActorUserId = "system", ActorUserName = "SLA Monitor", DataJson = JsonSerializer.Serialize(new { approvalRequestId = request.ApprovalRequestId, runId = request.RunId, requestedBy = request.RequestedBy, slaDeadlineUtc = request.SlaDeadlineUtc, breachedByMinutes = (int)(DateTime.UtcNow - request.SlaDeadlineUtc!.Value).TotalMinutes }) }, cancellationToken);
                await TrySendEscalationWebhookAsync(request, cancellationToken);
                await _approvalRequestRepository.PatchSlaBreachNotifiedAsync(request.ApprovalRequestId, DateTime.UtcNow, cancellationToken);
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                    _logger.LogWarning(ex, "SLA breach processing failed for ApprovalRequestId={Id}", LogSanitizer.Sanitize(request.ApprovalRequestId));
            }
        }
    }

    private async Task TrySendEscalationWebhookAsync(GovernanceApprovalRequest request, CancellationToken cancellationToken)
    {
        string? webhookUrl = _options.Value.ApprovalSlaEscalationWebhookUrl;
        if (string.IsNullOrWhiteSpace(webhookUrl))
            return;
        string sanitizedLabel = LogSanitizer.Sanitize(request.ApprovalRequestId);
        string payload = JsonSerializer.Serialize(new { approvalRequestId = request.ApprovalRequestId, runId = request.RunId, requestedBy = request.RequestedBy, slaDeadlineUtc = request.SlaDeadlineUtc, breachedByMinutes = (int)(DateTime.UtcNow - request.SlaDeadlineUtc!.Value).TotalMinutes });
        string? secret = _options.Value.EscalationWebhookSecret;
        ResiliencePipeline<HttpResponseMessage> retryPipeline = GovernanceSlaEscalationWebhookRetryPipeline.Create(_logger, sanitizedLabel);
        try
        {
            using HttpClient client = _httpClientFactory.CreateClient(SlaEscalationHttpClientName);
            using HttpResponseMessage response = await retryPipeline.ExecuteAsync(async ct =>
            {
                using HttpRequestMessage msg = BuildEscalationRequest(webhookUrl, payload, secret);
                return await client.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
                return;
            if (_logger.IsEnabled(LogLevel.Error))
                _logger.LogError("SLA escalation webhook failed after exhausting {MaxRetries} retry attempt(s): HTTP {StatusCode} for ApprovalRequestId={Id}.", GovernanceSlaEscalationWebhookRetryPipeline.MaxRetryAttempts, (int)response.StatusCode, sanitizedLabel);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
                _logger.LogError(ex, "SLA escalation webhook failed after exhausting {MaxRetries} retry attempt(s) for ApprovalRequestId={Id}.", GovernanceSlaEscalationWebhookRetryPipeline.MaxRetryAttempts, sanitizedLabel);
        }
    }

    private static HttpRequestMessage BuildEscalationRequest(string webhookUrl, string payload, string? secret)
    {
        HttpRequestMessage msg = new(HttpMethod.Post, webhookUrl);
        msg.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        if (!string.IsNullOrWhiteSpace(secret))
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
            byte[] hash = HMACSHA256.HashData(keyBytes, payloadBytes);
            string signature = Convert.ToHexStringLower(hash);
            msg.Headers.Add("X-ArchLucid-Signature", $"sha256={signature}");
        }

        return msg;
    }
}