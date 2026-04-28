using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Persistence.Tests.Data.Repositories;

public sealed class QueuedBackgroundJobPrepareResultTests
{
    [Fact]
    public void Record_holds_flags()
    {
        QueuedBackgroundJobPrepareResult r = new(
            ShouldRunExecutor: true,
            ShouldDeleteQueueMessageImmediately: false,
            WasUnknownJobId: true,
            RowWhenRunnable: null);

        r.ShouldRunExecutor.Should().BeTrue();
        r.ShouldDeleteQueueMessageImmediately.Should().BeFalse();
        r.WasUnknownJobId.Should().BeTrue();
        r.RowWhenRunnable.Should().BeNull();
    }
}
