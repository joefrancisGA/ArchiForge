using ArchiForge.Persistence.RelationalRead;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArchiForge.Persistence.Tests;

/// <summary>
/// Contract tests for <see cref="JsonFallbackPolicy"/> across all three
/// <see cref="PersistenceReadMode"/> values.
/// </summary>
[Trait("Category", "Unit")]
public sealed class JsonFallbackPolicyTests
{
    private static JsonFallbackPolicy Create(PersistenceReadMode mode) =>
        new(mode, NullLogger.Instance);

    // ── Default ────────────────────────────────────────────────────

    [Fact]
    public void Default_Constructor_UsesAllowJsonFallback()
    {
        JsonFallbackPolicy policy = new();

        policy.Mode.Should().Be(PersistenceReadMode.AllowJsonFallback);
        policy.AllowFallback.Should().BeTrue();
    }

    // ── AllowJsonFallback ──────────────────────────────────────────

    [Fact]
    public void AllowMode_RelationalRowsExist_ReturnsFalse()
    {
        JsonFallbackPolicy policy = Create(PersistenceReadMode.AllowJsonFallback);

        policy.EvaluateFallback(5, "Test.Slice").Should().BeFalse();
    }

    [Fact]
    public void AllowMode_NoRelationalRows_ReturnsTrue()
    {
        JsonFallbackPolicy policy = Create(PersistenceReadMode.AllowJsonFallback);

        policy.EvaluateFallback(0, "ContextSnapshot.CanonicalObjects").Should().BeTrue();
    }

    [Fact]
    public void AllowMode_NoRelationalRows_LogsDebug()
    {
        FakeLogger logger = new();
        JsonFallbackPolicy policy = new(PersistenceReadMode.AllowJsonFallback, logger);

        policy.EvaluateFallback(0, "ContextSnapshot.CanonicalObjects", "ContextSnapshot", "snap-1");

        logger.DebugCount.Should().Be(1);
        logger.LastDebugMessage.Should().Contain("ContextSnapshot.CanonicalObjects");
        logger.LastDebugMessage.Should().Contain("snap-1");
        logger.LastDebugMessage.Should().Contain("AllowJsonFallback");
    }

    [Fact]
    public void AllowMode_RelationalRowsExist_DoesNotLog()
    {
        FakeLogger logger = new();
        JsonFallbackPolicy policy = new(PersistenceReadMode.AllowJsonFallback, logger);

        policy.EvaluateFallback(5, "Test.Slice", "Test", "id-1");

        logger.TotalLogCount.Should().Be(0);
    }

    // ── WarnOnJsonFallback ─────────────────────────────────────────

    [Fact]
    public void WarnMode_RelationalRowsExist_ReturnsFalse()
    {
        JsonFallbackPolicy policy = Create(PersistenceReadMode.WarnOnJsonFallback);

        policy.EvaluateFallback(3, "Test.Slice").Should().BeFalse();
    }

    [Fact]
    public void WarnMode_NoRelationalRows_ReturnsTrue_AndLogsWarning()
    {
        FakeLogger logger = new();
        JsonFallbackPolicy policy = new(PersistenceReadMode.WarnOnJsonFallback, logger);

        bool result = policy.EvaluateFallback(0, "ContextSnapshot.Warnings", "ContextSnapshot", "abc-123");

        result.Should().BeTrue();
        logger.WarningCount.Should().Be(1);
        logger.LastWarningMessage.Should().Contain("ContextSnapshot.Warnings");
        logger.LastWarningMessage.Should().Contain("abc-123");
        logger.LastWarningMessage.Should().Contain("WarnOnJsonFallback");
    }

    // ── RequireRelational ──────────────────────────────────────────

    [Fact]
    public void RequireMode_RelationalRowsExist_ReturnsFalse()
    {
        JsonFallbackPolicy policy = Create(PersistenceReadMode.RequireRelational);

        policy.EvaluateFallback(1, "Test.Slice").Should().BeFalse();
    }

    [Fact]
    public void RequireMode_NoRelationalRows_Throws_WithEntityContext()
    {
        JsonFallbackPolicy policy = Create(PersistenceReadMode.RequireRelational);

        Action act = () => policy.EvaluateFallback(0, "ContextSnapshot.CanonicalObjects", "ContextSnapshot", "snap-42");

        RelationalDataMissingException ex = act.Should()
            .Throw<RelationalDataMissingException>().Which;
        ex.EntityType.Should().Be("ContextSnapshot");
        ex.EntityId.Should().Be("snap-42");
        ex.SliceName.Should().Be("ContextSnapshot.CanonicalObjects");
        ex.Message.Should().Contain("RequireRelational");
        ex.Message.Should().Contain("SqlRelationalBackfillService");
    }

    // ── AllowFallback computed property ────────────────────────────

    [Theory]
    [InlineData(PersistenceReadMode.AllowJsonFallback, true)]
    [InlineData(PersistenceReadMode.WarnOnJsonFallback, true)]
    [InlineData(PersistenceReadMode.RequireRelational, false)]
    public void AllowFallback_ReflectsMode(PersistenceReadMode mode, bool expected)
    {
        JsonFallbackPolicy policy = Create(mode);

        policy.AllowFallback.Should().Be(expected);
    }

    // ── ShouldFallbackToJson backward compat ──────────────────────

    [Fact]
    public void ShouldFallbackToJson_DelegatesToEvaluateFallback()
    {
        JsonFallbackPolicy allow = Create(PersistenceReadMode.AllowJsonFallback);
        JsonFallbackPolicy require = Create(PersistenceReadMode.RequireRelational);

        allow.ShouldFallbackToJson(0, "Slice").Should().BeTrue();
        Action act = () => require.ShouldFallbackToJson(0, "Slice");
        act.Should().Throw<RelationalDataMissingException>();
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

            if (logLevel != LogLevel.Debug) return;
            DebugCount++;
            LastDebugMessage = message;
        }
    }
}
