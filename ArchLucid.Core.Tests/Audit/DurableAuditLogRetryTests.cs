using ArchLucid.Core.Audit;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

namespace ArchLucid.Core.Tests.Audit;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class DurableAuditLogRetryTests
{
    [Fact]
    public async Task TryLogAsync_succeeds_on_first_attempt_without_delay()
    {
        int calls = 0;

        await DurableAuditLogRetry.TryLogAsync(
            _ =>
            {
                calls++;

                return Task.CompletedTask;
            },
            NullLogger.Instance,
            "test-op",
            CancellationToken.None,
            3);

        calls.Should().Be(1);
    }

    [Fact]
    public async Task TryLogAsync_retries_then_succeeds()
    {
        int calls = 0;

        await DurableAuditLogRetry.TryLogAsync(
            _ =>
            {
                calls++;

                return calls < 2 ? throw new InvalidOperationException("transient") : Task.CompletedTask;
            },
            NullLogger.Instance,
            "test-op",
            CancellationToken.None,
            3);

        calls.Should().Be(2);
    }

    [Fact]
    public async Task TryLogAsync_suppresses_after_max_attempts()
    {
        int calls = 0;

        await DurableAuditLogRetry.TryLogAsync(
            _ =>
            {
                calls++;

                throw new InvalidOperationException("fail");
            },
            NullLogger.Instance,
            "test-op",
            CancellationToken.None,
            2);

        calls.Should().Be(2);
    }

    [Fact]
    public async Task TryLogAsync_propagates_operation_canceled_from_write()
    {
        Func<Task> act = () => DurableAuditLogRetry.TryLogAsync(
            _ => throw new OperationCanceledException(),
            NullLogger.Instance,
            "test-op",
            CancellationToken.None);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
