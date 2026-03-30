using ArchiForge.Persistence.RelationalRead;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArchiForge.Persistence.Tests;

/// <summary>
/// 53R-5 regression tests for structured diagnostics emitted by <see cref="JsonFallbackPolicy"/>
/// and <see cref="RelationalFirstRead"/> under each <see cref="PersistenceReadMode"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class FallbackPolicyDiagnosticsTests
{
    // ── Multi-slice diagnostics ────────────────────────────────────

    [Fact]
    public void AllowMode_MultipleSliceFallbacks_LogsDebugForEach()
    {
        FakeLogger logger = new();
        JsonFallbackPolicy policy = new(PersistenceReadMode.AllowJsonFallback, logger);

        policy.EvaluateFallback(0, "ContextSnapshot.CanonicalObjects", "ContextSnapshot", "s1");
        policy.EvaluateFallback(0, "ContextSnapshot.Warnings", "ContextSnapshot", "s1");
        policy.EvaluateFallback(0, "ContextSnapshot.Errors", "ContextSnapshot", "s1");
        policy.EvaluateFallback(0, "ContextSnapshot.SourceHashes", "ContextSnapshot", "s1");

        logger.DebugCount.Should().Be(4);
        logger.WarningCount.Should().Be(0);
    }

    [Fact]
    public void WarnMode_MultipleSliceFallbacks_LogsWarningForEach()
    {
        FakeLogger logger = new();
        JsonFallbackPolicy policy = new(PersistenceReadMode.WarnOnJsonFallback, logger);

        policy.EvaluateFallback(0, "ContextSnapshot.CanonicalObjects", "ContextSnapshot", "s1");
        policy.EvaluateFallback(0, "ContextSnapshot.Warnings", "ContextSnapshot", "s1");

        logger.WarningCount.Should().Be(2);
        logger.DebugCount.Should().Be(0);
    }

    [Fact]
    public void WarnMode_MixedRelationalAndFallback_OnlyLogsForFallback()
    {
        FakeLogger logger = new();
        JsonFallbackPolicy policy = new(PersistenceReadMode.WarnOnJsonFallback, logger);

        policy.EvaluateFallback(5, "ContextSnapshot.CanonicalObjects", "ContextSnapshot", "s1");
        policy.EvaluateFallback(0, "ContextSnapshot.Warnings", "ContextSnapshot", "s1");
        policy.EvaluateFallback(3, "ContextSnapshot.Errors", "ContextSnapshot", "s1");

        logger.WarningCount.Should().Be(1);
        logger.TotalLogCount.Should().Be(1);
    }

    // ── Structured log content assertions ──────────────────────────

    [Theory]
    [InlineData("ContextSnapshot.CanonicalObjects", "ContextSnapshot", "snap-1")]
    [InlineData("GoldenManifest.Provenance", "GoldenManifest", "mfst-42")]
    [InlineData("FindingsSnapshot.Findings", "FindingsSnapshot", "find-99")]
    public void AllowMode_DebugLog_ContainsAllDiagnosticFields(string sliceName, string entityType, string entityId)
    {
        FakeLogger logger = new();
        JsonFallbackPolicy policy = new(PersistenceReadMode.AllowJsonFallback, logger);

        policy.EvaluateFallback(0, sliceName, entityType, entityId);

        logger.LastDebugMessage.Should().Contain(sliceName);
        logger.LastDebugMessage.Should().Contain(entityType);
        logger.LastDebugMessage.Should().Contain(entityId);
        logger.LastDebugMessage.Should().Contain("AllowJsonFallback");
        logger.LastDebugMessage.Should().Contain("SqlRelationalBackfillService");
    }

    [Theory]
    [InlineData("GraphSnapshot.EdgeProperties", "GraphSnapshot", "graph-7")]
    [InlineData("ArtifactBundle.Artifacts", "ArtifactBundle", "bundle-3")]
    public void WarnMode_WarningLog_ContainsAllDiagnosticFields(string sliceName, string entityType, string entityId)
    {
        FakeLogger logger = new();
        JsonFallbackPolicy policy = new(PersistenceReadMode.WarnOnJsonFallback, logger);

        policy.EvaluateFallback(0, sliceName, entityType, entityId);

        logger.LastWarningMessage.Should().Contain(sliceName);
        logger.LastWarningMessage.Should().Contain(entityType);
        logger.LastWarningMessage.Should().Contain(entityId);
        logger.LastWarningMessage.Should().Contain("WarnOnJsonFallback");
        logger.LastWarningMessage.Should().Contain("SqlRelationalBackfillService");
    }

    // ── RequireRelational exception contract ───────────────────────

    [Theory]
    [InlineData("ContextSnapshot", "snap-1", "ContextSnapshot.CanonicalObjects")]
    [InlineData("GoldenManifest", "mfst-2", "GoldenManifest.Decisions")]
    [InlineData("GraphSnapshot", "g-3", "GraphSnapshot.Nodes")]
    public void RequireMode_Exception_HasActionableMessage(string entityType, string entityId, string sliceName)
    {
        JsonFallbackPolicy policy = new(PersistenceReadMode.RequireRelational, NullLogger.Instance);

        Action act = () => policy.EvaluateFallback(0, sliceName, entityType, entityId);

        RelationalDataMissingException ex = act.Should().Throw<RelationalDataMissingException>().Which;
        ex.EntityType.Should().Be(entityType);
        ex.EntityId.Should().Be(entityId);
        ex.SliceName.Should().Be(sliceName);
        ex.Message.Should().Contain("RequireRelational");
        ex.Message.Should().Contain("SqlRelationalBackfillService");
        ex.Message.Should().Contain(entityType);
        ex.Message.Should().Contain(entityId);
        ex.Message.Should().Contain(sliceName);
    }

    [Fact]
    public void RequireMode_NoLog_WhenThrowing()
    {
        FakeLogger logger = new();
        JsonFallbackPolicy policy = new(PersistenceReadMode.RequireRelational, logger);

        Action act = () => policy.EvaluateFallback(0, "Test.Slice", "Test", "id-1");
        act.Should().Throw<RelationalDataMissingException>();

        logger.TotalLogCount.Should().Be(0);
    }

    // ── RelationalFirstRead multi-mode regression ──────────────────

    [Fact]
    public async Task ReadSliceAsync_AllowMode_ReturnsJsonFallbackData()
    {
        JsonFallbackPolicy policy = new(PersistenceReadMode.AllowJsonFallback, NullLogger.Instance);

        List<string> result = await RelationalFirstRead.ReadSliceAsync(
            relationalRowCount: 0,
            "ContextSnapshot.Warnings",
            () => Task.FromResult(new List<string> { "relational" }),
            () => ["json-warning-1", "json-warning-2"],
            () => [],
            policy,
            "ContextSnapshot", "snap-1");

        result.Should().Equal("json-warning-1", "json-warning-2");
    }

    [Fact]
    public async Task ReadSliceAsync_WarnMode_ReturnsJsonFallbackData_AndLogsWarning()
    {
        FakeLogger logger = new();
        JsonFallbackPolicy policy = new(PersistenceReadMode.WarnOnJsonFallback, logger);

        List<string> result = await RelationalFirstRead.ReadSliceAsync(
            relationalRowCount: 0,
            "ContextSnapshot.Warnings",
            () => Task.FromResult(new List<string> { "relational" }),
            () => ["json-legacy"],
            () => [],
            policy,
            "ContextSnapshot", "snap-1");

        result.Should().Equal("json-legacy");
        logger.WarningCount.Should().Be(1);
        logger.LastWarningMessage.Should().Contain("ContextSnapshot.Warnings");
    }

    [Fact]
    public async Task ReadSliceAsync_RequireMode_Throws_WithSliceAndEntityContext()
    {
        JsonFallbackPolicy policy = new(PersistenceReadMode.RequireRelational, NullLogger.Instance);

        Func<Task> act = () => RelationalFirstRead.ReadSliceAsync(
            relationalRowCount: 0,
            "GoldenManifest.Assumptions",
            () => Task.FromResult(new List<string> { "relational" }),
            () => ["json"],
            () => [],
            policy,
            "GoldenManifest", "mfst-42");

        RelationalDataMissingException ex = (await act.Should().ThrowAsync<RelationalDataMissingException>()).Which;
        ex.SliceName.Should().Be("GoldenManifest.Assumptions");
        ex.EntityType.Should().Be("GoldenManifest");
        ex.EntityId.Should().Be("mfst-42");
    }

    [Fact]
    public async Task ReadSliceAsync_RequireMode_WithRelationalRows_Succeeds()
    {
        JsonFallbackPolicy policy = new(PersistenceReadMode.RequireRelational, NullLogger.Instance);

        List<string> result = await RelationalFirstRead.ReadSliceAsync(
            relationalRowCount: 3,
            "ContextSnapshot.CanonicalObjects",
            () => Task.FromResult(new List<string> { "relational-data" }),
            () => throw new InvalidOperationException("should not call JSON fallback"),
            () => throw new InvalidOperationException("should not call empty default"),
            policy,
            "ContextSnapshot", "snap-1");

        result.Should().Equal("relational-data");
    }

    // ── Test helper ────────────────────────────────────────────────

    private sealed class FakeLogger : ILogger
    {
        public int WarningCount { get; private set; }

        public int DebugCount { get; private set; }

        public int TotalLogCount { get; private set; }

        public string LastWarningMessage { get; private set; } = "";

        public string LastDebugMessage { get; private set; } = "";

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            TotalLogCount++;
            string message = formatter(state, exception);

            if (logLevel == LogLevel.Warning)
            {
                WarningCount++;
                LastWarningMessage = message;
            }

            if (logLevel == LogLevel.Debug)
            {
                DebugCount++;
                LastDebugMessage = message;
            }
        }
    }
}
