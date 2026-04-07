using ArchLucid.Core.Transactions;

using Moq;

namespace ArchLucid.TestSupport;

/// <summary>
/// Shared <see cref="IArchLucidUnitOfWorkFactory"/> test double: in-memory persistence path (<see cref="IArchLucidUnitOfWork.SupportsExternalTransaction"/> is false).
/// </summary>
public static class ArchLucidUnitOfWorkTestDoubles
{
    public static IArchLucidUnitOfWorkFactory InMemoryModeFactory()
    {
        Mock<IArchLucidUnitOfWork> uow = new();
        uow.SetupGet(x => x.SupportsExternalTransaction).Returns(false);
        uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        Mock<IArchLucidUnitOfWorkFactory> factory = new();
        factory.Setup(x => x.CreateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(uow.Object);

        return factory.Object;
    }
}
