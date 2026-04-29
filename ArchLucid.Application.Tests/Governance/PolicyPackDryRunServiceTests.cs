using System.Collections.Immutable;
using System.Text.Json;

using ArchLucid.Application.Governance;
using ArchLucid.Application.Pilots;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Findings;
using ArchLucid.Contracts.Governance;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Llm.Redaction;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Application.Tests.Governance;

[Trait("Category", "Unit")]
public sealed class PolicyPackDryRunServiceTests
{
    private static readonly Guid PolicyPackId = Guid.Parse("11112222-3333-4444-5555-666677778888");

    [Fact]
    public async Task EvaluateAsync_DefaultsPageSizeTo20WhenNotProvided()
    {
        PolicyPackDryRunService sut = BuildSutWithRunCount(50);

        PolicyPackDryRunResponse response = await sut.EvaluateAsync(
            PolicyPackId,
            new Dictionary<string, string>(),
            BuildRunIds(50),
            pageSize: null,
            page: null,
            CancellationToken.None);

        response.PageSize.Should().Be(IPolicyPackDryRunService.DefaultPageSize);
        response.PageSize.Should().Be(20);
        response.Items.Should().HaveCount(20);
    }

    [Fact]
    public async Task EvaluateAsync_ClampsPageSizeToServerMaximum()
    {
        PolicyPackDryRunService sut = BuildSutWithRunCount(120);

        PolicyPackDryRunResponse response = await sut.EvaluateAsync(
            PolicyPackId,
            new Dictionary<string, string>(),
            BuildRunIds(120),
            pageSize: 999,
            page: 1,
            CancellationToken.None);

        response.PageSize.Should().Be(IPolicyPackDryRunService.MaxPageSize);
        response.PageSize.Should().Be(100);
        response.Items.Should().HaveCount(100);
    }

    [Fact]
    public async Task EvaluateAsync_ClampsPageBelowOneToFirstPage()
    {
        PolicyPackDryRunService sut = BuildSutWithRunCount(5);

        PolicyPackDryRunResponse response = await sut.EvaluateAsync(
            PolicyPackId,
            new Dictionary<string, string>(),
            BuildRunIds(5),
            pageSize: 2,
            page: -7,
            CancellationToken.None);

        response.Page.Should().Be(1);
        response.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task EvaluateAsync_ProposedThresholdsRunThroughRedactor()
    {
        StubRedactor redactor = new();
        PolicyPackDryRunService sut = BuildSut(
            runCount: 1,
            redactor: redactor);

        Dictionary<string, string> proposed = new()
        {
            { "maxCriticalFindings", "0" },
            { "comment", "ping me at alice@example.com about ssn 111-22-3333" },
        };

        PolicyPackDryRunResponse response = await sut.EvaluateAsync(
            PolicyPackId,
            proposed,
            ["run-1"],
            pageSize: 20,
            page: 1,
            CancellationToken.None);

        redactor.Calls.Should().Be(1);
        response.ProposedThresholdsRedactedJson.Should().Contain("[REDACTED]");
        response.ProposedThresholdsRedactedJson.Should().NotContain("alice@example.com");
        response.ProposedThresholdsRedactedJson.Should().NotContain("111-22-3333");
    }

    [Fact]
    public async Task EvaluateAsync_TalliesWouldBlockUsingProposedThresholds()
    {
        FakeRunDetailQueryService runs = new();
        runs.AddRun("run-clean", critical: 0, high: 0, medium: 0);
        runs.AddRun("run-noisy", critical: 3, high: 0, medium: 0);

        FakeDeltaComputer computer = new();

        PolicyPackDryRunService sut = new(
            runs,
            computer,
            new StubRedactor(),
            Mock.Of<IAuditService>(),
            NullLogger<PolicyPackDryRunService>.Instance);

        Dictionary<string, string> proposed = new()
        {
            { PolicyPackDryRunSupportedThresholdKeys.MaxCriticalFindings, "0" },
        };

        PolicyPackDryRunResponse response = await sut.EvaluateAsync(
            PolicyPackId,
            proposed,
            ["run-clean", "run-noisy"],
            pageSize: 20,
            page: 1,
            CancellationToken.None);

        response.DeltaCounts.Evaluated.Should().Be(2);
        response.DeltaCounts.WouldBlock.Should().Be(1);
        response.DeltaCounts.WouldAllow.Should().Be(1);
        response.DeltaCounts.RunMissing.Should().Be(0);

        PolicyPackDryRunRunItem noisy = response.Items.Single(i => i.RunId == "run-noisy");
        noisy.WouldBlock.Should().BeTrue();
        noisy.ThresholdOutcomes.Should().ContainSingle()
            .Which.WouldBreach.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_MarksUnknownRunIdsAsMissingAndExcludesFromTallies()
    {
        FakeRunDetailQueryService runs = new();
        runs.AddRun("run-clean", critical: 0, high: 0, medium: 0);

        FakeDeltaComputer computer = new();

        PolicyPackDryRunService sut = new(
            runs,
            computer,
            new StubRedactor(),
            Mock.Of<IAuditService>(),
            NullLogger<PolicyPackDryRunService>.Instance);

        PolicyPackDryRunResponse response = await sut.EvaluateAsync(
            PolicyPackId,
            new Dictionary<string, string>(),
            ["run-clean", "ghost-run"],
            pageSize: 20,
            page: 1,
            CancellationToken.None);

        response.DeltaCounts.Evaluated.Should().Be(2);
        response.DeltaCounts.RunMissing.Should().Be(1);
        response.Items.Should().Contain(i => i.RunId == "ghost-run" && i.RunMissing);
    }

    [Fact]
    public async Task EvaluateAsync_PersistsAuditRowWithRedactedPayload()
    {
        StubAuditService audit = new();
        FakeRunDetailQueryService runs = new();
        runs.AddRun("run-clean", critical: 0, high: 0, medium: 0);

        FakeDeltaComputer computer = new();

        PolicyPackDryRunService sut = new(
            runs,
            computer,
            new StubRedactor(),
            audit,
            NullLogger<PolicyPackDryRunService>.Instance);

        Dictionary<string, string> proposed = new()
        {
            { PolicyPackDryRunSupportedThresholdKeys.MaxCriticalFindings, "1" },
            { "comment", "alice@example.com" },
        };

        await sut.EvaluateAsync(
            PolicyPackId,
            proposed,
            ["run-clean"],
            pageSize: 20,
            page: 1,
            CancellationToken.None);

        audit.LastEvent.Should().NotBeNull();
        audit.LastEvent!.EventType.Should().Be(AuditEventTypes.GovernanceDryRunRequested);
        audit.LastEvent.DataJson.Should().Contain("[REDACTED]");
        audit.LastEvent.DataJson.Should().NotContain("alice@example.com");

        using JsonDocument doc = JsonDocument.Parse(audit.LastEvent.DataJson);
        doc.RootElement.GetProperty("policyPackId").GetGuid().Should().Be(PolicyPackId);
        doc.RootElement.GetProperty("evaluatedRunIds").EnumerateArray()
            .Select(e => e.GetString()).Should().BeEquivalentTo("run-clean");
        doc.RootElement.GetProperty("deltaCounts").GetProperty("evaluated").GetInt32().Should().Be(1);
    }

    private static List<string> BuildRunIds(int count) =>
        Enumerable.Range(1, count).Select(i => $"run-{i:0000}").ToList();

    private static PolicyPackDryRunService BuildSutWithRunCount(int runCount) =>
        BuildSut(runCount, new StubRedactor());

    private static PolicyPackDryRunService BuildSut(int runCount, IPromptRedactor redactor)
    {
        FakeRunDetailQueryService runs = new();

        for (int i = 1; i <= runCount; i++)
            runs.AddRun($"run-{i:0000}", critical: 0, high: 0, medium: 0);

        FakeDeltaComputer computer = new();

        return new PolicyPackDryRunService(
            runs,
            computer,
            redactor,
            Mock.Of<IAuditService>(),
            NullLogger<PolicyPackDryRunService>.Instance);
    }

    private sealed class StubRedactor : IPromptRedactor
    {
        public int Calls
        {
            get; private set;
        }

        public PromptRedactionOutcome Redact(string? input)
        {
            Calls++;
            string text = input ?? string.Empty;
            text = text.Replace("alice@example.com", "[REDACTED]")
                .Replace("111-22-3333", "[REDACTED]");

            return new PromptRedactionOutcome(text, ImmutableDictionary<string, int>.Empty);
        }
    }

    private sealed class StubAuditService : IAuditService
    {
        public AuditEvent? LastEvent
        {
            get; private set;
        }

        public Task LogAsync(AuditEvent auditEvent, CancellationToken ct)
        {
            LastEvent = auditEvent;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRunDetailQueryService : IRunDetailQueryService
    {
        private readonly Dictionary<string, ArchitectureRunDetail> _byId = new(StringComparer.OrdinalIgnoreCase);

        public void AddRun(string runId, int critical, int high, int medium)
        {
            List<ArchitectureFinding> findings = [];

            for (int i = 0; i < critical; i++)
                findings.Add(new ArchitectureFinding { FindingId = $"f-c-{i}", Severity = FindingSeverity.Critical });
            for (int i = 0; i < high; i++)
                findings.Add(new ArchitectureFinding { FindingId = $"f-h-{i}", Severity = FindingSeverity.Error });
            for (int i = 0; i < medium; i++)
                findings.Add(new ArchitectureFinding { FindingId = $"f-m-{i}", Severity = FindingSeverity.Warning });

            _byId[runId] = new ArchitectureRunDetail
            {
                Run = new ArchitectureRun { RunId = runId, RequestId = $"req-{runId}", CreatedUtc = DateTime.UtcNow },
                Results = [new AgentResult { Findings = findings }],
            };
        }

        public Task<ArchitectureRunDetail?> GetRunDetailAsync(string runId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_byId.TryGetValue(runId, out ArchitectureRunDetail? detail) ? detail : null);

        public Task<IReadOnlyList<RunSummary>> ListRunSummariesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RunSummary>>([]);

        public Task<(IReadOnlyList<RunSummary> Items, bool HasMore, string? NextCursor)> ListRunSummariesKeysetAsync(
            string? cursor,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<RunSummary>, bool, string?)>(([], false, null));
    }

    /// <summary>
    ///     Standalone delta computer that mirrors <see cref="FakeRunDetailQueryService" /> findings into the
    ///     <see cref="PilotRunDeltas.FindingsBySeverity" /> shape the production service consumes.
    /// </summary>
    private sealed class FakeDeltaComputer : IPilotRunDeltaComputer
    {
        public Task<PilotRunDeltas> ComputeAsync(ArchitectureRunDetail detail, CancellationToken cancellationToken = default)
        {
            if (detail is null)
                throw new ArgumentNullException(nameof(detail));

            List<KeyValuePair<string, int>> bySeverity = detail.Results
                .SelectMany(r => r.Findings)
                .GroupBy(static f => f.Severity.ToString(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
                .ToList();

            return Task.FromResult(new PilotRunDeltas
            {
                RunCreatedUtc = detail.Run.CreatedUtc,
                FindingsBySeverity = bySeverity,
            });
        }
    }
}
