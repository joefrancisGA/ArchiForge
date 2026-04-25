using ArchLucid.Persistence.Connections;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests;

[Trait("Category", "Unit")]
public sealed class SqlReadReplicaConnectionStringResolverTests
{
    [Fact]
    public void Resolve_AuthorityRunList_Prefers_legacy_key_when_both_set()
    {
        SqlReadReplicaSettings settings = new()
        {
            AuthorityRunListReadsConnectionString = "run-list",
            FailoverGroupReadOnlyListenerConnectionString = "failover"
        };

        string? actual =
            SqlReadReplicaConnectionStringResolver.Resolve(ReadReplicaQueryRoute.AuthorityRunList, settings);

        actual.Should().Be("run-list");
    }

    [Fact]
    public void Resolve_AuthorityRunList_Uses_failover_when_legacy_empty()
    {
        SqlReadReplicaSettings settings = new() { FailoverGroupReadOnlyListenerConnectionString = "failover" };

        string? actual =
            SqlReadReplicaConnectionStringResolver.Resolve(ReadReplicaQueryRoute.AuthorityRunList, settings);

        actual.Should().Be("failover");
    }

    [Fact]
    public void Resolve_Governance_Prefers_failover_when_both_set()
    {
        SqlReadReplicaSettings settings = new()
        {
            AuthorityRunListReadsConnectionString = "run-list",
            FailoverGroupReadOnlyListenerConnectionString = "failover"
        };

        string? actual =
            SqlReadReplicaConnectionStringResolver.Resolve(ReadReplicaQueryRoute.GovernanceResolution, settings);

        actual.Should().Be("failover");
    }

    [Fact]
    public void Resolve_Governance_Falls_back_to_legacy_when_failover_empty()
    {
        SqlReadReplicaSettings settings = new() { AuthorityRunListReadsConnectionString = "run-list" };

        string? actual =
            SqlReadReplicaConnectionStringResolver.Resolve(ReadReplicaQueryRoute.GovernanceResolution, settings);

        actual.Should().Be("run-list");
    }
}
