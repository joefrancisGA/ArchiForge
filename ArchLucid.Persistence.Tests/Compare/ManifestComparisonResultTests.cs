namespace ArchLucid.Persistence.Tests.Compare;

public sealed class ManifestComparisonResultTests
{
    [SkippableFact]
    public void Diff_kind_counts_reflect_Diffs_collection()
    {
        ManifestComparisonResult result = new()
        {
            Diffs =
            [
                new DiffItem { DiffKind = DiffKind.Added },
                new DiffItem { DiffKind = DiffKind.Added },
                new DiffItem { DiffKind = DiffKind.Removed },
                new DiffItem { DiffKind = DiffKind.Changed }
            ]
        };

        result.AddedCount.Should().Be(2);
        result.RemovedCount.Should().Be(1);
        result.ChangedCount.Should().Be(1);
    }
}
