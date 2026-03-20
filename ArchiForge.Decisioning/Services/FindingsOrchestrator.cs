using System.Diagnostics;
using ArchiForge.Decisioning.Findings.Serialization;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Models;
using Microsoft.Extensions.Logging;

namespace ArchiForge.Decisioning.Services;

public partial class FindingsOrchestrator(
    IEnumerable<IFindingEngine> engines,
    IFindingPayloadValidator validator,
    ILogger<FindingsOrchestrator> logger)
    : IFindingsOrchestrator
{
    public FindingsOrchestrator(IEnumerable<IFindingEngine> engines)
        : this(engines, new NoOpFindingPayloadValidator(), SilentLogger.Instance)
    {
    }

    public FindingsOrchestrator(
        IEnumerable<IFindingEngine> engines,
        IFindingPayloadValidator validator)
        : this(engines, validator, SilentLogger.Instance)
    {
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Finding engine failed: EngineType={EngineType} Category={Category} DurationMs={DurationMs}")]
    private partial void LogEngineFailed(Exception ex, string engineType, string category, long durationMs);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Finding engine completed: EngineType={EngineType} Category={Category} DurationMs={DurationMs} FindingsCount={Count}")]
    private partial void LogEngineCompleted(string engineType, string category, long durationMs, int count);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Findings snapshot built: FindingsSnapshotId={SnapshotId} TotalFindings={Total} SchemaVersion={SchemaVersion}")]
    private partial void LogSnapshotBuilt(Guid snapshotId, int total, int schemaVersion);

    public async Task<FindingsSnapshot> GenerateFindingsSnapshotAsync(
        Guid runId,
        Guid contextSnapshotId,
        GraphSnapshot graphSnapshot,
        CancellationToken ct)
    {
        var allFindings = new List<Finding>();

        foreach (var engine in engines)
        {
            var sw = Stopwatch.StartNew();
            IReadOnlyList<Finding> findings;
            try
            {
                findings = await engine.AnalyzeAsync(graphSnapshot, ct);
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogEngineFailed(ex, engine.EngineType, engine.Category, sw.ElapsedMilliseconds);
                throw;
            }

            sw.Stop();
            LogEngineCompleted(engine.EngineType, engine.Category, sw.ElapsedMilliseconds, findings.Count);

            foreach (var finding in findings)
            {
                if (string.IsNullOrWhiteSpace(finding.Category))
                    finding.Category = engine.Category;

                validator.Validate(finding);

                if (!string.Equals(finding.Category, engine.Category, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Finding category '{finding.Category}' did not match engine category '{engine.Category}' for engine '{engine.EngineType}'.");
                }

                allFindings.Add(finding);
            }
        }

        var snapshot = new FindingsSnapshot
        {
            FindingsSnapshotId = Guid.NewGuid(),
            RunId = runId,
            ContextSnapshotId = contextSnapshotId,
            GraphSnapshotId = graphSnapshot.GraphSnapshotId,
            CreatedUtc = DateTime.UtcNow,
            Findings = allFindings,
            SchemaVersion = FindingsSchema.CurrentSnapshotVersion
        };

        FindingsSnapshotMigrator.Apply(snapshot);

        LogSnapshotBuilt(snapshot.FindingsSnapshotId, snapshot.Findings.Count, snapshot.SchemaVersion);

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
