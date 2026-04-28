using ArchLucid.Persistence.Repositories;

namespace ArchLucid.Persistence.Tests.Repositories;

public sealed class RunConcurrencyConflictExceptionTests
{
    [Fact]
    public void Message_and_RunId_set()
    {
        Guid runId = Guid.NewGuid();

        RunConcurrencyConflictException ex = new(runId);

        ex.RunId.Should().Be(runId);
        ex.Message.Should().Contain(runId.ToString("D"));
    }
}
