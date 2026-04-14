using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Audit;
using ArchLucid.Persistence.Data.Repositories;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Governance;

/// <summary>Detects SLA-breached pending approval requests and sends escalation notifications.</summary>
public sealed class ApprovalSlaMonitor
{
    private readonly IGovernanceApprovalRequestRepository _approvalRequestRepository;
    private readonly IAuditService _auditService;
    private readonly IOptions<PreCommitGovernanceGateOptions> _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApprovalSlaMonitor> _logger;

    public ApprovalSlaMonitor(
        IGovernanceApprovalRequestRepository approvalRequestRepository,
        IAuditService auditService,
        IOptions<PreCommitGovernanceGateOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<ApprovalSlaMonitor> logger)
    {
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
        {
            return;
        }

        IReadOnlyList<GovernanceApprovalRequest> breached = await _approvalRequestRepository
            .GetPendingSlaBreachedAsync(DateTime.UtcNow, cancellationToken);

        foreach (GovernanceApprovalRequest request in breached)
        {
            try
            {
                await _auditService.LogAsync(
                    new AuditEvent
                    {
                        EventType = AuditEventTypes.GovernanceApprovalSlaBreached,
                        ActorUserId = "system",
                        ActorUserName = "SLA Monitor",
                        DataJson = JsonSerializer.Serialize(new
                        {
                            approvalRequestId = request.ApprovalRequestId,
                            runId = request.RunId,
                            requestedBy = request.RequestedBy,
                            slaDeadlineUtc = request.SlaDeadlineUtc,
                            breachedByMinutes = (int)(DateTime.UtcNow - request.SlaDeadlineUtc!.Value).TotalMinutes,
                        }),
                    },
                    cancellationToken);

                await TrySendEscalationWebhookAsync(request, cancellationToken);

                await _approvalRequestRepository.PatchSlaBreachNotifiedAsync(
                    request.ApprovalRequestId,
                    DateTime.UtcNow,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SLA breach processing failed for ApprovalRequestId={Id}", request.ApprovalRequestId);
            }
        }
    }

    private async Task TrySendEscalationWebhookAsync(GovernanceApprovalRequest request, CancellationToken cancellationToken)
    {
        string? webhookUrl = _options.Value.ApprovalSlaEscalationWebhookUrl;

        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            return;
        }

        try
        {
            string payload = JsonSerializer.Serialize(new
            {
                approvalRequestId = request.ApprovalRequestId,
                runId = request.RunId,
                requestedBy = request.RequestedBy,
                slaDeadlineUtc = request.SlaDeadlineUtc,
                breachedByMinutes = (int)(DateTime.UtcNow - request.SlaDeadlineUtc!.Value).TotalMinutes,
            });

            using HttpClient client = _httpClientFactory.CreateClient("SlaEscalation");
            using HttpRequestMessage msg = new(HttpMethod.Post, webhookUrl);
            msg.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            string? secret = _options.Value.EscalationWebhookSecret;

            if (!string.IsNullOrWhiteSpace(secret))
            {
                byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
                byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
                byte[] hash = HMACSHA256.HashData(keyBytes, payloadBytes);
                string signature = Convert.ToHexStringLower(hash);
                msg.Headers.Add("X-ArchLucid-Signature", $"sha256={signature}");
            }

            HttpResponseMessage response = await client.SendAsync(msg, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "SLA escalation webhook returned {StatusCode} for ApprovalRequestId={Id}",
                    (int)response.StatusCode,
                    request.ApprovalRequestId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SLA escalation webhook failed for ApprovalRequestId={Id}", request.ApprovalRequestId);
        }
    }
}
