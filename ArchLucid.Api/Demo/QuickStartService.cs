using System.Text.Json;

using ArchLucid.Application;
using ArchLucid.Application.Common;
using ArchLucid.Application.Runs.Orchestration;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Findings;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Scoping;
using ArchLucid.Host.Core.Demo;
using ArchLucid.Persistence.Serialization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ArchLucid.Api.Demo;

/// <summary>
///     Orchestrates anonymous marketing quick-start: create → forced-simulator execute → commit under the demo scope.
/// </summary>
public sealed class QuickStartService(
    IArchitectureRunCreateOrchestrator architectureRunCreateOrchestrator,
    [FromKeyedServices(ArchitectureRunExecuteOrchestrationKeys.QuickStartForcedSimulator)]
    IArchitectureRunExecuteOrchestrator quickStartExecuteOrchestrator,
    IArchitectureRunCommitOrchestrator architectureRunCommitOrchestrator,
    IAuditService auditService,
    IActorContext actorContext,
    IScopeContextProvider scopeContextProvider,
    IOptionsMonitor<PublicSiteOptions> publicSiteOptions,
    ILogger<QuickStartService> logger)
{
    private readonly IActorContext
        _actorContext = actorContext ?? throw new ArgumentNullException(nameof(actorContext));

    private readonly IArchitectureRunCommitOrchestrator _architectureRunCommitOrchestrator =
        architectureRunCommitOrchestrator ?? throw new ArgumentNullException(nameof(architectureRunCommitOrchestrator));

    private readonly IArchitectureRunCreateOrchestrator _architectureRunCreateOrchestrator =
        architectureRunCreateOrchestrator ?? throw new ArgumentNullException(nameof(architectureRunCreateOrchestrator));

    private readonly IAuditService
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

    private readonly ILogger<QuickStartService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IOptionsMonitor<PublicSiteOptions> _publicSiteOptions =
        publicSiteOptions ?? throw new ArgumentNullException(nameof(publicSiteOptions));

    private readonly IArchitectureRunExecuteOrchestrator _quickStartExecuteOrchestrator =
        quickStartExecuteOrchestrator ?? throw new ArgumentNullException(nameof(quickStartExecuteOrchestrator));

    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

    public async Task<DemoQuickStartResponse> RunAsync(DemoQuickStartRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        ArchitectureRequest architectureRequest = BuildArchitectureRequest(request);

        using IDisposable _ = AmbientScopeContext.Push(DemoScopes.BuildDemoScope());

        string actor = _actorContext.GetActor();

        CreateRunResult created =
            await _architectureRunCreateOrchestrator
                .CreateRunAsync(architectureRequest, null, cancellationToken)
                .ConfigureAwait(false);

        string runId = created.Run.RunId;

        ExecuteRunResult executed =
            await _quickStartExecuteOrchestrator.ExecuteRunAsync(runId, cancellationToken)
                .ConfigureAwait(false);

        await LogRunSubmittedAuditAsync(actor, runId, cancellationToken).ConfigureAwait(false);

        CommitRunResult committed =
            await _architectureRunCommitOrchestrator.CommitRunAsync(runId, cancellationToken).ConfigureAwait(false);

        await LogQuickStartCompletedAuditAsync(actor, scopeContextProvider.GetCurrentScope(), runId, cancellationToken)
            .ConfigureAwait(false);

        string manifestVersion = committed.Manifest.Metadata.ManifestVersion;
        string trimmedBaseUrl = _publicSiteOptions.CurrentValue.BaseUrl.TrimEnd('/');
        string runDetailUrl = $"{trimmedBaseUrl}/runs/{Uri.EscapeDataString(runId)}";

        if (_logger.IsEnabled(LogLevel.Information))

            _logger.LogInformation(
                "Demo quick-start pipeline completed RunId={RunId} ManifestVersion={ManifestVersion}",
                runId,
                manifestVersion);

        return new DemoQuickStartResponse
        {
            RunId = runId,
            ManifestId = manifestVersion,
            TopFindings = SelectTopFindingSummaries(executed.Results, 3),
            RunDetailUrl = runDetailUrl
        };
    }

    private static ArchitectureRequest BuildArchitectureRequest(DemoQuickStartRequest inbound)
    {
        string? trimmedPresetKey = inbound.PresetId?.Trim();
        QuickStartPresets.PresetPayload? preset = null;

        if (!string.IsNullOrWhiteSpace(trimmedPresetKey))
        {
            if (!QuickStartPresets.TryGet(trimmedPresetKey, out QuickStartPresets.PresetPayload resolved))
                throw new InvalidOperationException($"Unknown presetId '{trimmedPresetKey}'.");

            preset = resolved;
        }

        string rawDescription = (inbound.Description ?? string.Empty).Trim();

        string body;

        if (preset is not null)
        {
            body = rawDescription.Length > 0
                ? $"{preset.ArchitectureDescription} Additional notes: {rawDescription}"
                : preset.ArchitectureDescription;
        }
        else
        {
            if (rawDescription.Length == 0)
                throw new InvalidOperationException("Description is required when presetId is not provided.");

            body = rawDescription;
        }

        List<string> capabilityAccumulator = [];

        if (preset?.RequiredCapabilities is { Count: > 0 })
            capabilityAccumulator.AddRange(preset.RequiredCapabilities.Select(static c => c.Trim()));

        capabilityAccumulator.Add("Demonstration-only analysis path");

        List<string> distinctCapabilities =
        [
            .. capabilityAccumulator
                .Where(static c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
        ];

        List<string> constraints;

        if (preset is null)
            constraints =
            [
                .. QuickStartPresets.LogicalScopePins,
                "Free-text marketing quick-start path inputs"
            ];

        else
            constraints =
            [
                .. preset.Constraints.Where(static s => !string.IsNullOrWhiteSpace(s))
            ];

        List<string> topologyHints =
        [
            "Document synchronous vs asynchronous interaction edges",
            "Surface blast-radius boundaries explicitly"
        ];

        List<string> securityHints =
        [
            "Default-deny egress where feasible",
            "Centralize cryptographic key custody"
        ];

        return new ArchitectureRequest
        {
            RequestId = $"qs-{Guid.NewGuid():N}",
            Description = NormalizeDescription(body),
            SystemName = preset?.SystemDisplayName.Trim().Length > 0
                ? preset.SystemDisplayName.Trim()
                : "Quick Start Architecture",
            Environment = "sandbox",
            CloudProvider = CloudProvider.Azure,
            TopologyHints = topologyHints,
            SecurityBaselineHints = securityHints,
            RequiredCapabilities = distinctCapabilities,
            Constraints =
            [
                .. constraints.Where(static s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
            ]
        };
    }

    private static string NormalizeDescription(string text)
    {
        const int minimum = 10;

        string trimmedOuter = text.Trim();

        return trimmedOuter.Length >= minimum ? trimmedOuter : $"{trimmedOuter} Quick-start marketing summary.";
    }

    private static List<DemoQuickStartFindingSummary> SelectTopFindingSummaries(
        IReadOnlyList<AgentResult> results,
        int limit)
    {
        IEnumerable<ArchitectureFinding> ordered =
            results.SelectMany(static r => r.Findings).OrderByDescending(static f => f.Severity);

        List<DemoQuickStartFindingSummary> picked = [];

        foreach (ArchitectureFinding finding in ordered)
        {
            if (picked.Count >= limit)
                break;

            string title = DisplayTitle(finding);
            picked.Add(new DemoQuickStartFindingSummary { Title = title, Severity = finding.Severity.ToString() });
        }

        return picked;
    }

    private static string DisplayTitle(ArchitectureFinding finding)
    {
        if (!string.IsNullOrWhiteSpace(finding.Message))
            return finding.Message.Trim();

        return string.IsNullOrWhiteSpace(finding.Category) ? "Finding" : finding.Category.Trim();
    }

    private async Task LogRunSubmittedAuditAsync(string actor, string runId, CancellationToken cancellationToken)
    {
        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        Guid? runGuid = TryParseRunGuidForAudit(runId);

        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.RunSubmitted,
                ActorUserId = actor,
                ActorUserName = actor,
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ProjectId = scope.ProjectId,
                RunId = runGuid
            },
            cancellationToken).ConfigureAwait(false);
    }

    private async Task LogQuickStartCompletedAuditAsync(
        string actor,
        ScopeContext scope,
        string runId,
        CancellationToken cancellationToken)
    {
        Guid? runGuid = TryParseRunGuidForAudit(runId);

        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.RunCompleted,
                ActorUserId = actor,
                ActorUserName = actor,
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ProjectId = scope.ProjectId,
                RunId = runGuid,
                DataJson = JsonSerializer.Serialize(
                    new { runId, source = "demo-quickstart" },
                    AuditJsonSerializationOptions.Instance)
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static Guid? TryParseRunGuidForAudit(string runId)
    {
        if (Guid.TryParseExact(runId, "N", out Guid parsed))
            return parsed;

        return Guid.TryParse(runId, out parsed) ? parsed : null;
    }
}
