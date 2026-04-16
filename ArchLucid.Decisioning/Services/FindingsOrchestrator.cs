using System.Diagnostics;

using ArchLucid.Core.Diagnostics;
using ArchLucid.Decisioning.Findings.Serialization;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Models;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Decisioning.Services;

public partial class FindingsOrchestrator(
    IEnumerable<IFindingEngine> engines,
    IFindingPayloadValidator validator,
    ILogger<FindingsOrchestrator> logger,
    TimeProvider? timeProvider = null)
    : IFindingsOrchestrator
{
    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;

    /// <summary>
    /// Initialises the orchestrator without a payload validator or logger.
    /// </summary>
    /// <remarks>
    /// No payload validation is performed when using this overload.
    /// Prefer the primary constructor with an explicit <see cref="IFindingPayloadValidator"/>
    /// injected from the DI container.
    /// </remarks>
    [Obsolete("Use the primary constructor that accepts IFindingPayloadValidator and ILogger<FindingsOrchestrator>. " +
              "This overload silently skips payload validation.")]
    public FindingsOrchestrator(IEnumerable<IFindingEngine> engines)
        : this(engines, new NoOpFindingPayloadValidator(), SilentLogger.Instance, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initialises the orchestrator with a validator but without a logger.
    /// </summary>
    /// <remarks>
    /// No structured logging is emitted when using this overload.
    /// Prefer the primary constructor that also accepts <see cref="ILogger{TCategoryName}"/>.
    /// </remarks>
    [Obsolete("Use the primary constructor that also accepts ILogger<FindingsOrchestrator>. " +
              "This overload discards all log output.")]
    public FindingsOrchestrator(
        IEnumerable<IFindingEngine> engines,
        IFindingPayloadValidator validator)
        : this(engines, validator, SilentLogger.Instance, TimeProvider.System)
    {
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Finding engine failed: RunId={RunId} EngineType={EngineType} Category={Category} DurationMs={DurationMs}")]
    private partial void LogEngineFailed(Exception ex, Guid runId, string engineType, string category, long durationMs);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Finding engine completed: RunId={RunId} EngineType={EngineType} Category={Category} DurationMs={DurationMs} FindingsCount={Count}")]
    private partial void LogEngineCompleted(Guid runId, string engineType, string category, long durationMs, int count);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Findings snapshot built: RunId={RunId} FindingsSnapshotId={SnapshotId} TotalFindings={Total} SchemaVersion={SchemaVersion}")]
    private partial void LogSnapshotBuilt(Guid runId, Guid snapshotId, int total, int schemaVersion);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Findings snapshot built with engine failures: RunId={RunId} FailedEngineCount={FailedEngineCount}")]
    private partial void LogPartialEngineFailures(Guid runId, int failedEngineCount);

    public async Task<FindingsSnapshot> GenerateFindingsSnapshotAsync(
        Guid runId,
        Guid contextSnapshotId,
        GraphSnapshot graphSnapshot,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(graphSnapshot);

        List<Finding> allFindings = [];
        List<FindingEngineFailure> engineFailures = [];
        List<Exception> engineExceptions = [];
        int successfulEngineInvocations = 0;

        foreach (IFindingEngine engine in engines)
        {
            Stopwatch sw = Stopwatch.StartNew();
            IReadOnlyList<Finding> findings;
            try
            {
                findings = await engine.AnalyzeAsync(graphSnapshot, ct);
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogEngineFailed(ex, runId, engine.EngineType, engine.Category, sw.ElapsedMilliseconds);
                ArchLucidInstrumentation.RecordFindingEngineFailure(engine.EngineType, engine.Category);
                engineExceptions.Add(ex);
                engineFailures.Add(
                    new FindingEngineFailure
                    {
                        EngineType = engine.EngineType,
                        Category = engine.Category,
                        ErrorMessage = ex.Message,
                        ExceptionType = ex.GetType().Name,
                        DurationMs = sw.ElapsedMilliseconds,
                        OccurredUtc = _clock.GetUtcNow().UtcDateTime,
                    });

                continue;
            }

            successfulEngineInvocations++;
            sw.Stop();
            LogEngineCompleted(runId, engine.EngineType, engine.Category, sw.ElapsedMilliseconds, findings.Count);

            foreach (Finding finding in findings)
            {
                if (string.IsNullOrWhiteSpace(finding.Category))
                    finding.Category = engine.Category;

                validator.Validate(finding);

                if (!string.Equals(finding.Category, engine.Category, StringComparison.OrdinalIgnoreCase))

                    throw new InvalidOperationException(
                        $"Finding category '{finding.Category}' did not match engine category '{engine.Category}' for engine '{engine.EngineType}'.");


                allFindings.Add(finding);
            }
        }

        if (successfulEngineInvocations == 0 && engineExceptions.Count > 0)
            throw new AggregateException("All finding engines failed for this snapshot.", engineExceptions);

        List<Finding> dedupedFindings = allFindings
            .GroupBy(x => $"{x.FindingType}|{x.Title}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        FindingsSnapshot snapshot = new()
        {
            FindingsSnapshotId = Guid.NewGuid(),
            RunId = runId,
            ContextSnapshotId = contextSnapshotId,
            GraphSnapshotId = graphSnapshot.GraphSnapshotId,
            CreatedUtc = _clock.GetUtcNow().UtcDateTime,
            Findings = dedupedFindings,
            EngineFailures = engineFailures,
            SchemaVersion = FindingsSchema.CurrentSnapshotVersion
        };

        FindingsSnapshotMigrator.Apply(snapshot);

        if (engineFailures.Count > 0 && successfulEngineInvocations > 0)
            LogPartialEngineFailures(runId, engineFailures.Count);

        LogSnapshotBuilt(runId, snapshot.FindingsSnapshotId, snapshot.Findings.Count, snapshot.SchemaVersion);

        return snapshot;
    }

    private sealed class NoOpFindingPayloadValidator : IFindingPayloadValidator
    {
        public void Validate(Finding finding)
        {
        }
    }

    /// <summary>ILogger that discards all output (no dependency on NullLogger from logging abstractions).</summary>
    private sealed class SilentLogger : ILogger<FindingsOrchestrator>
    {
        public static readonly ILogger<FindingsOrchestrator> Instance = new SilentLogger();

        private SilentLogger()
        {
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }

        private sealed class NullScope : IDisposable
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
