using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Persistence.Coordination.Compare;
using ArchLucid.Persistence.Queries;

using FluentAssertions;

using Moq;

namespace ArchLucid.Persistence.Tests.Compare;

public sealed class AuthorityCompareServiceAddRunDiffTests
{
    [Fact]
    public void AddRunDiff_appends_when_values_differ()
    {
        Mock<IGoldenManifestRepository> manifests = new();
        Mock<IAuthorityQueryService> queries = new();
        AuthorityCompareService svc = new(manifests.Object, queries.Object);
        List<DiffItem> diffs = [];

        svc.AddRunDiff(diffs, "Run", "ProjectId", "a", "b");

        diffs.Should().ContainSingle();
        diffs[0].DiffKind.Should().Be(DiffKind.Changed);
        diffs[0].BeforeValue.Should().Be("a");
        diffs[0].AfterValue.Should().Be("b");
    }

    [Fact]
    public void AddRunDiff_skips_when_values_equal_ordinal()
    {
        Mock<IGoldenManifestRepository> manifests = new();
        Mock<IAuthorityQueryService> queries = new();
        AuthorityCompareService svc = new(manifests.Object, queries.Object);

        List<DiffItem> diffs = [];
        svc.AddRunDiff(diffs, "Run", "k", "same", "same");

        diffs.Should().BeEmpty();
    }
}
