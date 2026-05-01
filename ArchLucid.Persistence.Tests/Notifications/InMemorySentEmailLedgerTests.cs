using ArchLucid.Core.Notifications;
using ArchLucid.Persistence.Notifications;

namespace ArchLucid.Persistence.Tests.Notifications;

public sealed class InMemorySentEmailLedgerTests
{
    [SkippableFact]
    public async Task TryRecordSentAsync_false_when_key_blank()
    {
        InMemorySentEmailLedger sut = new();
        SentEmailLedgerEntry entry = new("  ", Guid.NewGuid(), "t", "p", null);

        bool ok = await sut.TryRecordSentAsync(entry, CancellationToken.None);

        ok.Should().BeFalse();
    }

    [SkippableFact]
    public async Task TryRecordSentAsync_true_then_false_duplicate()
    {
        InMemorySentEmailLedger sut = new();
        SentEmailLedgerEntry entry = new("key-1", Guid.NewGuid(), "t", "p", null);

        bool first = await sut.TryRecordSentAsync(entry, CancellationToken.None);
        bool second = await sut.TryRecordSentAsync(entry, CancellationToken.None);

        first.Should().BeTrue();
        second.Should().BeFalse();
    }

    [SkippableFact]
    public async Task TryRecordSentAsync_trims_key()
    {
        InMemorySentEmailLedger sut = new();
        SentEmailLedgerEntry a = new("  abc  ", Guid.NewGuid(), "t", "p", null);
        SentEmailLedgerEntry b = new("abc", Guid.NewGuid(), "t", "p", null);

        await sut.TryRecordSentAsync(a, CancellationToken.None);
        bool dup = await sut.TryRecordSentAsync(b, CancellationToken.None);

        dup.Should().BeFalse();
    }
}
