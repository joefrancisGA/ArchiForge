using System.Diagnostics.Metrics;

using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.BlobStore;
using ArchLucid.Persistence.Data.Repositories;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.AgentRuntime.Tests.AgentExecutionTraceRecorder;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class AgentExecutionTraceRecorderReproTests
{
    private sealed class FixedScopeProvider : IScopeContextProvider
    {
        public ScopeContext GetCurrentScope() =>
            new()
            {
                TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                WorkspaceId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                ProjectId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
            };
    }

    private sealed class NoOpAuditService : IAuditService
    {
        public Task LogAsync(AuditEvent auditEvent, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class SpyAuditService : IAuditService
    {
        public AuditEvent? LastEvent
        {
            get; private set;
        }

        public Task LogAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
        {
            LastEvent = auditEvent;

            return Task.CompletedTask;
        }
    }

    private sealed class RecordingAuditService : IAuditService
    {
        public List<AuditEvent> Events { get; } = [];

        public Task LogAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
        {
            Events.Add(auditEvent);

            return Task.CompletedTask;
        }
    }

    /// <summary>Forces SQL inline patch to throw while delegating other trace operations to memory.</summary>
    private sealed class InlinePatchThrowsRepository : IAgentExecutionTraceRepository
    {
        private readonly InMemoryAgentExecutionTraceRepository _inner = new();

        public Task CreateAsync(AgentExecutionTrace trace, CancellationToken cancellationToken = default) =>
            _inner.CreateAsync(trace, cancellationToken);

        public Task PatchBlobStorageFieldsAsync(
            string traceId,
            string? fullSystemPromptBlobKey,
            string? fullUserPromptBlobKey,
            string? fullResponseBlobKey,
            CancellationToken cancellationToken = default) =>
            _inner.PatchBlobStorageFieldsAsync(
                traceId,
                fullSystemPromptBlobKey,
                fullUserPromptBlobKey,
                fullResponseBlobKey,
                cancellationToken);

        public Task PatchBlobUploadFailedAsync(string traceId, bool failed, CancellationToken cancellationToken = default) =>
            _inner.PatchBlobUploadFailedAsync(traceId, failed, cancellationToken);

        public Task PatchInlinePromptFallbackAsync(
            string traceId,
            string? fullSystemPromptInline,
            string? fullUserPromptInline,
            string? fullResponseInline,
            CancellationToken cancellationToken = default) =>
            throw new IOException("simulated mandatory inline SQL patch failure");

        public Task PatchInlineFallbackFailedAsync(string traceId, bool failed, CancellationToken cancellationToken = default) =>
            _inner.PatchInlineFallbackFailedAsync(traceId, failed, cancellationToken);

        public Task<AgentExecutionTrace?> GetByTraceIdAsync(string traceId, CancellationToken cancellationToken = default) =>
            _inner.GetByTraceIdAsync(traceId, cancellationToken);

        public Task<IReadOnlyList<AgentExecutionTrace>> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default) =>
            _inner.GetByRunIdAsync(runId, cancellationToken);

        public Task<(IReadOnlyList<AgentExecutionTrace> Traces, int TotalCount)> GetPagedByRunIdAsync(
            string runId,
            int offset,
            int limit,
            CancellationToken cancellationToken = default) =>
            _inner.GetPagedByRunIdAsync(runId, offset, limit, cancellationToken);

        public Task<IReadOnlyList<AgentExecutionTrace>> GetByTaskIdAsync(string taskId, CancellationToken cancellationToken = default) =>
            _inner.GetByTaskIdAsync(taskId, cancellationToken);
    }

    [Fact]
    public async Task RecordAsync_persists_prompt_repro_fields()
    {
        InMemoryAgentExecutionTraceRepository repo = new();
        AgentRuntime.AgentExecutionTraceRecorder sut = CreateRecorder(repo);

        AgentPromptReproMetadata meta = new("topology-system", "1.0.0", "abc123deadbeef", "pilot-a");

        await sut.RecordAsync(
            "run-1",
            "task-1",
            AgentType.Topology,
            "system",
            "user",
            "{}",
            "{}",
            parseSucceeded: true,
            errorMessage: null,
            meta);

        IReadOnlyList<AgentExecutionTrace> list = await repo.GetByRunIdAsync("run-1");

        AgentExecutionTrace t = list.Should().ContainSingle().Subject;
        t.PromptTemplateId.Should().Be("topology-system");
        t.PromptTemplateVersion.Should().Be("1.0.0");
        t.SystemPromptContentSha256.Should().Be("abc123deadbeef");
        t.PromptReleaseLabel.Should().Be("pilot-a");
    }

    [Fact]
    public async Task RecordAsync_when_model_metadata_null_uses_unspecified_sentinels()
    {
        InMemoryAgentExecutionTraceRepository repo = new();
        AgentRuntime.AgentExecutionTraceRecorder sut = CreateRecorder(repo);

        await sut.RecordAsync(
            "run-1",
            "task-1",
            AgentType.Topology,
            "system",
            "user",
            "{}",
            "{}",
            parseSucceeded: true,
            errorMessage: null,
            promptRepro: null,
            inputTokenCount: null,
            outputTokenCount: null,
            modelDeploymentName: null,
            modelVersion: null);

        IReadOnlyList<AgentExecutionTrace> list = await repo.GetByRunIdAsync("run-1");
        AgentExecutionTrace t = list.Should().ContainSingle().Subject;
        t.ModelDeploymentName.Should().Be(AgentExecutionTraceModelMetadata.UnspecifiedDeploymentName);
        t.ModelVersion.Should().Be(AgentExecutionTraceModelMetadata.UnspecifiedModelVersion);
    }

    [Fact]
    public async Task RecordAsync_persists_token_counts_and_estimated_cost_when_enabled()
    {
        InMemoryAgentExecutionTraceRepository repo = new();
        IOptions<LlmCostEstimationOptions> opts = Options.Create(
            new LlmCostEstimationOptions
            {
                Enabled = true,
                InputUsdPerMillionTokens = 1m,
                OutputUsdPerMillionTokens = 2m,
            });
        AgentRuntime.AgentExecutionTraceRecorder sut = CreateRecorder(repo, costOptions: opts);

        await sut.RecordAsync(
            "run-1",
            "task-1",
            AgentType.Topology,
            "system",
            "user",
            "{}",
            "{}",
            parseSucceeded: true,
            errorMessage: null,
            promptRepro: null,
            inputTokenCount: 1_000_000,
            outputTokenCount: 500_000);

        IReadOnlyList<AgentExecutionTrace> list = await repo.GetByRunIdAsync("run-1");
        AgentExecutionTrace t = list.Should().ContainSingle().Subject;
        t.InputTokenCount.Should().Be(1_000_000);
        t.OutputTokenCount.Should().Be(500_000);
        t.EstimatedCostUsd.Should().Be(2m);
    }

    [Fact]
    public async Task RecordAsync_sets_blob_keys_when_store_succeeds()
    {
        InMemoryAgentExecutionTraceRepository repo = new();
        AgentRuntime.AgentExecutionTraceRecorder sut = CreateRecorder(repo);

        await sut.RecordAsync(
            "run-1",
            "task-1",
            AgentType.Topology,
            "full-system",
            "full-user",
            "full-response",
            "{}",
            parseSucceeded: true,
            errorMessage: null);

        IReadOnlyList<AgentExecutionTrace> list = await repo.GetByRunIdAsync("run-1");
        AgentExecutionTrace t = list.Should().ContainSingle().Subject;
        t.FullSystemPromptBlobKey.Should().NotBeNullOrEmpty();
        t.FullUserPromptBlobKey.Should().NotBeNullOrEmpty();
        t.FullResponseBlobKey.Should().NotBeNullOrEmpty();
        t.BlobUploadFailed.Should().BeFalse();
    }

    [Fact]
    public async Task RecordAsync_when_simulator_execution_skips_blob_writes()
    {
        InMemoryAgentExecutionTraceRepository repo = new();
        Mock<IArtifactBlobStore> blobMock = new();
        AgentRuntime.AgentExecutionTraceRecorder sut = CreateRecorder(repo, blobStore: blobMock.Object);

        await sut.RecordAsync(
            "run-1",
            "task-1",
            AgentType.Topology,
            "full-system",
            "full-user",
            "full-response",
            "{}",
            parseSucceeded: true,
            errorMessage: null,
            isSimulatorExecution: true);

        blobMock.Verify(
            b => b.WriteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        IReadOnlyList<AgentExecutionTrace> list = await repo.GetByRunIdAsync("run-1");
        AgentExecutionTrace t = list.Should().ContainSingle().Subject;
        t.FullSystemPromptBlobKey.Should().BeNull();
        t.FullUserPromptBlobKey.Should().BeNull();
        t.FullResponseBlobKey.Should().BeNull();
    }

    [Fact]
    public async Task RecordAsync_when_blob_writes_fail_persists_inline_text_for_missing_blobs()
    {
        InMemoryAgentExecutionTraceRepository repo = new();
        Mock<IArtifactBlobStore> blobMock = new();
        blobMock
            .Setup(b => b.WriteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string _, string path, string _, CancellationToken _) =>
                path.Contains("user-prompt", StringComparison.OrdinalIgnoreCase)
                    ? Task.FromResult<string>(null!)
                    : Task.FromResult($"ok://{path}"));

        AgentRuntime.AgentExecutionTraceRecorder sut = CreateRecorder(repo, blobStore: blobMock.Object);

        await sut.RecordAsync(
            "run-1",
            "task-1",
            AgentType.Topology,
            "full-system",
            "full-user",
            "full-response",
            "{}",
            parseSucceeded: true,
            errorMessage: null);

        IReadOnlyList<AgentExecutionTrace> list = await repo.GetByRunIdAsync("run-1");
        AgentExecutionTrace t = list.Should().ContainSingle().Subject;
        t.BlobUploadFailed.Should().BeTrue();
        t.FullUserPromptInline.Should().Be("full-user");
        t.FullSystemPromptInline.Should().BeNull();
        t.FullResponseInline.Should().BeNull();
    }

    [Fact]
    public async Task RecordAsync_when_blob_writes_fail_sets_blob_upload_failed_and_audits()
    {
        InMemoryAgentExecutionTraceRepository repo = new();
        SpyAuditService spyAudit = new();
        Mock<IArtifactBlobStore> blobMock = new();
        blobMock
            .Setup(b => b.WriteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("simulated blob failure"));

        AgentRuntime.AgentExecutionTraceRecorder sut = CreateRecorder(
            repo,
            blobStore: blobMock.Object,
            auditService: spyAudit);

        await sut.RecordAsync(
            "run-1",
            "task-1",
            AgentType.Compliance,
            "s",
            "u",
            "r",
            "{}",
            parseSucceeded: true,
            errorMessage: null);

        IReadOnlyList<AgentExecutionTrace> list = await repo.GetByRunIdAsync("run-1");
        AgentExecutionTrace t = list.Should().ContainSingle().Subject;
        t.BlobUploadFailed.Should().BeTrue();
        t.FullSystemPromptInline.Should().Be("s");
        t.FullUserPromptInline.Should().Be("u");
        t.FullResponseInline.Should().Be("r");
        spyAudit.LastEvent.Should().NotBeNull();
        spyAudit.LastEvent!.EventType.Should().Be(AuditEventTypes.AgentTraceBlobPersistenceFailed);
        spyAudit.LastEvent.DataJson.Should().Contain("upload_failed");
    }

    [Fact]
    public async Task RecordAsync_retries_blob_write_three_times_before_abandoning_failed_part()
    {
        InMemoryAgentExecutionTraceRepository repo = new();
        Mock<IArtifactBlobStore> blobMock = new();
        blobMock
            .Setup(b => b.WriteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string _, string path, string _, CancellationToken _) =>
                path.Contains("system-prompt", StringComparison.OrdinalIgnoreCase)
                    ? Task.FromException<string>(new IOException("simulated transient blob failure"))
                    : Task.FromResult($"ok://{path}"));

        AgentRuntime.AgentExecutionTraceRecorder sut = CreateRecorder(repo, blobStore: blobMock.Object);

        await sut.RecordAsync(
            "run-1",
            "task-1",
            AgentType.Topology,
            "full-system",
            "full-user",
            "full-response",
            "{}",
            parseSucceeded: true,
            errorMessage: null);

        blobMock.Verify(
            b => b.WriteAsync(
                It.IsAny<string>(),
                It.Is<string>(p => p.Contains("system-prompt", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));

        blobMock.Verify(
            b => b.WriteAsync(
                It.IsAny<string>(),
                It.Is<string>(p => p.Contains("user-prompt", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once());

        blobMock.Verify(
            b => b.WriteAsync(
                It.IsAny<string>(),
                It.Is<string>(p => p.Contains("response.txt", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task RecordAsync_when_each_blob_exhausts_retries_increments_archlucid_agent_trace_blob_upload_failures_total()
    {
        _ = ArchLucidInstrumentation.AgentTraceBlobUploadFailuresTotal;

        InMemoryAgentExecutionTraceRepository repo = new();
        Mock<IArtifactBlobStore> blobMock = new();
        blobMock
            .Setup(b => b.WriteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("always fail"));

        using BlobUploadFailureMeasurementCapture capture = BlobUploadFailureMeasurementCapture.Start();

        AgentRuntime.AgentExecutionTraceRecorder sut = CreateRecorder(repo, blobStore: blobMock.Object);

        await sut.RecordAsync(
            "run-1",
            "task-1",
            AgentType.Cost,
            "s",
            "u",
            "r",
            "{}",
            parseSucceeded: true,
            errorMessage: null);

        IReadOnlyList<LongMeasurementRecord> failures = capture.MeasurementsFor("archlucid_agent_trace_blob_upload_failures_total");
        failures.Should().HaveCount(3);
        failures.Sum(m => m.Value).Should().Be(3);

        HashSet<string> blobTypes = failures
            .SelectMany(m => m.Tags.Where(t => t.Key == "blob_type").Select(t => (string)t.Value!))
            .ToHashSet(StringComparer.Ordinal);

        blobTypes.Should().BeEquivalentTo(["system_prompt", "user_prompt", "response"]);
    }

    [Fact]
    public async Task RecordAsync_when_all_blobs_fail_increments_prompt_inline_fallback_total_per_blob_type()
    {
        _ = ArchLucidInstrumentation.AgentTracePromptInlineFallbacksTotal;

        InMemoryAgentExecutionTraceRepository repo = new();
        Mock<IArtifactBlobStore> blobMock = new();
        blobMock
            .Setup(b => b.WriteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("always fail"));

        using PromptInlineFallbackMeasurementCapture capture = PromptInlineFallbackMeasurementCapture.Start();

        AgentRuntime.AgentExecutionTraceRecorder sut = CreateRecorder(repo, blobStore: blobMock.Object);

        await sut.RecordAsync(
            "run-1",
            "task-1",
            AgentType.Topology,
            "s",
            "u",
            "r",
            "{}",
            parseSucceeded: true,
            errorMessage: null);

        IReadOnlyList<LongMeasurementRecord> inline = capture.MeasurementsFor("archlucid_agent_trace_prompt_inline_fallback_total");
        inline.Should().HaveCount(3);
        inline.Sum(m => m.Value).Should().Be(3);

        HashSet<string> blobTypes = inline
            .SelectMany(m => m.Tags.Where(t => t.Key == "blob_type").Select(t => (string)t.Value!))
            .ToHashSet(StringComparer.Ordinal);

        blobTypes.Should().BeEquivalentTo(["system_prompt", "user_prompt", "response"]);
    }

    [Fact]
    public async Task RecordAsync_when_inline_sql_patch_throws_sets_inline_fallback_failed_and_audits()
    {
        InlinePatchThrowsRepository repo = new();
        RecordingAuditService recordingAudit = new();
        Mock<IArtifactBlobStore> blobMock = new();
        blobMock
            .Setup(b => b.WriteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("blob down"));

        AgentRuntime.AgentExecutionTraceRecorder sut = CreateRecorder(
            repo,
            blobStore: blobMock.Object,
            auditService: recordingAudit);

        await sut.RecordAsync(
            "run-1",
            "task-1",
            AgentType.Topology,
            "s",
            "u",
            "r",
            "{}",
            parseSucceeded: true,
            errorMessage: null);

        IReadOnlyList<AgentExecutionTrace> list = await repo.GetByRunIdAsync("run-1");
        AgentExecutionTrace t = list.Should().ContainSingle().Subject;
        t.InlineFallbackFailed.Should().BeTrue();

        recordingAudit.Events.Should().Contain(e => e.EventType == AuditEventTypes.AgentTraceInlineFallbackFailed);
        recordingAudit.Events.Should().Contain(e => e.EventType == AuditEventTypes.AgentTraceBlobPersistenceFailed);
    }

    private sealed class BlobUploadFailureMeasurementCapture : IDisposable
    {
        private readonly MeterListener _listener = new();

        private readonly List<LongMeasurementRecord> _longMeasures = [];

        private BlobUploadFailureMeasurementCapture()
        {
            _listener.InstrumentPublished = OnInstrumentPublished;
            _listener.SetMeasurementEventCallback<long>(OnMeasurementLong);
            _listener.Start();
        }

        public static BlobUploadFailureMeasurementCapture Start() => new();

        public void Dispose() => _listener.Dispose();

        public IReadOnlyList<LongMeasurementRecord> MeasurementsFor(string instrumentName) =>
            _longMeasures.Where(m => m.Name == instrumentName).ToList();

        private void OnInstrumentPublished(Instrument instrument, MeterListener meterListener)
        {
            if (instrument.Meter.Name != ArchLucidInstrumentation.MeterName)
            {
                return;
            }

            if (instrument.Name == "archlucid_agent_trace_blob_upload_failures_total")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        }

        private void OnMeasurementLong(
            Instrument instrument,
            long measurement,
            ReadOnlySpan<KeyValuePair<string, object?>> tags,
            object? state)
        {
            _ = state;
            List<KeyValuePair<string, object?>> tagList = [];

            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagList.Add(tag);
            }

            _longMeasures.Add(new LongMeasurementRecord(instrument.Name, measurement, tagList));
        }
    }

    private sealed class PromptInlineFallbackMeasurementCapture : IDisposable
    {
        private readonly MeterListener _listener = new();

        private readonly List<LongMeasurementRecord> _longMeasures = [];

        private PromptInlineFallbackMeasurementCapture()
        {
            _listener.InstrumentPublished = OnInstrumentPublished;
            _listener.SetMeasurementEventCallback<long>(OnMeasurementLong);
            _listener.Start();
        }

        public static PromptInlineFallbackMeasurementCapture Start() => new();

        public void Dispose() => _listener.Dispose();

        public IReadOnlyList<LongMeasurementRecord> MeasurementsFor(string instrumentName) =>
            _longMeasures.Where(m => m.Name == instrumentName).ToList();

        private void OnInstrumentPublished(Instrument instrument, MeterListener meterListener)
        {
            if (instrument.Meter.Name != ArchLucidInstrumentation.MeterName)
            {
                return;
            }

            if (instrument.Name == "archlucid_agent_trace_prompt_inline_fallback_total")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        }

        private void OnMeasurementLong(
            Instrument instrument,
            long measurement,
            ReadOnlySpan<KeyValuePair<string, object?>> tags,
            object? state)
        {
            _ = state;
            List<KeyValuePair<string, object?>> tagList = [];

            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagList.Add(tag);
            }

            _longMeasures.Add(new LongMeasurementRecord(instrument.Name, measurement, tagList));
        }
    }

    private readonly record struct LongMeasurementRecord(
        string Name,
        long Value,
        IReadOnlyList<KeyValuePair<string, object?>> Tags);

    private static AgentRuntime.AgentExecutionTraceRecorder CreateRecorder(
        IAgentExecutionTraceRepository repo,
        IOptions<LlmCostEstimationOptions>? costOptions = null,
        IArtifactBlobStore? blobStore = null,
        IAuditService? auditService = null)
    {
        IOptions<LlmCostEstimationOptions> cost = costOptions ?? Options.Create(new LlmCostEstimationOptions { Enabled = false });
        ServiceCollection services = new();
        services.AddScoped<IAgentExecutionTraceRepository>(_ => repo);
        services.AddSingleton(blobStore ?? new InMemoryArtifactBlobStore());
        services.AddSingleton(cost);
        services.AddSingleton(Options.Create(new AgentExecutionTraceStorageOptions()));
        services.AddSingleton<ILlmCostEstimator, LlmCostEstimator>();
        services.AddSingleton<IAuditService>(_ => auditService ?? new NoOpAuditService());
        services.AddSingleton<IScopeContextProvider, FixedScopeProvider>();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.None));
        services.AddScoped<AgentRuntime.AgentExecutionTraceRecorder>();
        ServiceProvider provider = services.BuildServiceProvider();
        IServiceScope scope = provider.CreateScope();

        return scope.ServiceProvider.GetRequiredService<AgentRuntime.AgentExecutionTraceRecorder>();
    }
}
