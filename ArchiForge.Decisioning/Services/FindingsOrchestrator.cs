using System.Diagnostics;
using ArchiForge.Decisioning.Findings.Serialization;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Models;
using Microsoft.Extensions.Logging;

namespace ArchiForge.Decisioning.Services;

public class FindingsOrchestrator(
    IEnumerable<IFindingEngine> engines,
    IFindingsSnapshotRepository repository,
    IFindingPayloadValidator validator,
    ILogger<FindingsOrchestrator> logger)
    : IFindingsOrchestrator
{
    public FindingsOrchestrator(
        IEnumerable<IFindingEngine> engines,
        IFindingsSnapshotRepository repository)
        : this(engines, repository, new NoOpFindingPayloadValidator(), SilentLogger.Instance)
    {
    }

    public FindingsOrchestrator(
        IEnumerable<IFindingEngine> engines,
        IFindingsSnapshotRepository repository,
        IFindingPayloadValidator validator)
        : this(engines, repository, validator, SilentLogger.Instance)
    {
    }

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
                logger.LogError(ex,
                    "Finding engine failed: EngineType={EngineType} Category={Category} DurationMs={DurationMs}",
                    engine.EngineType, engine.Category, sw.ElapsedMilliseconds);
                throw;
            }

            sw.Stop();
            logger.LogInformation(
                "Finding engine completed: EngineType={EngineType} Category={Category} DurationMs={DurationMs} FindingsCount={Count}",
                engine.EngineType, engine.Category, sw.ElapsedMilliseconds, findings.Count);

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

        logger.LogInformation(
            "Findings snapshot built: FindingsSnapshotId={SnapshotId} TotalFindings={Total} SchemaVersion={SchemaVersion}",
            snapshot.FindingsSnapshotId, snapshot.Findings.Count, snapshot.SchemaVersion);

        await repository.SaveAsync(snapshot, ct);

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

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

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
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
