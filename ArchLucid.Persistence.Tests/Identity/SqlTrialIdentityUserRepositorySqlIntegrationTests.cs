using ArchLucid.Application.Identity;
using ArchLucid.Core.Identity;
using ArchLucid.Persistence.Identity;
using ArchLucid.Persistence.Tests.Tenancy;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Identity;

/// <summary>
///     <see cref="SqlTrialIdentityUserRepository" /> against SQL (<c>dbo.IdentityUsers</c> + migration <strong>131</strong>
///     handoff columns).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class SqlTrialIdentityUserRepositorySqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    [SkippableFact]
    public async Task TryLinkLocalIdentityToEntraAsync_persists_oid_and_utc()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        TestSqlConnectionFactory factory = new(fixture.ConnectionString);
        SqlTrialIdentityUserRepository sut = new(factory);
        string email = "linktest+" + Guid.NewGuid().ToString("N")[..8] + "@example.com";
        string normalized = TrialEmailNormalizer.Normalize(email);
        DateTimeOffset confirmWindowEnd = DateTimeOffset.UtcNow.AddDays(1);

        await sut.CreatePendingUserAsync(
            normalized,
            email,
            passwordHash: "HASH",
            securityStamp: "sec",
            concurrencyStamp: "con",
            emailConfirmationTokenHash: "tokhash",
            emailConfirmationExpiresUtc: confirmWindowEnd,
            CancellationToken.None);

        (await sut.TryConfirmEmailAsync(normalized, "tokhash", DateTimeOffset.UtcNow, CancellationToken.None)).Should()
            .BeTrue();

        (await sut.TryLinkLocalIdentityToEntraAsync(normalized, "oid-a1b2", CancellationToken.None)).Should().BeTrue();

        TrialIdentityUserRecord? row = await sut.GetByNormalizedEmailAsync(normalized, CancellationToken.None);

        row.Should().NotBeNull();
        row!.LinkedEntraOid.Should().Be("oid-a1b2");
        row.LinkedUtc.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task TryLinkLocalIdentityToEntraAsync_false_when_oid_conflict()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        TestSqlConnectionFactory factory = new(fixture.ConnectionString);
        SqlTrialIdentityUserRepository sut = new(factory);
        string email = "conflict+" + Guid.NewGuid().ToString("N")[..8] + "@example.com";
        string normalized = TrialEmailNormalizer.Normalize(email);

        await sut.CreatePendingUserAsync(
            normalized,
            email,
            "HASH",
            "s",
            "c",
            "th",
            DateTimeOffset.UtcNow.AddDays(1),
            CancellationToken.None);

        (await sut.TryConfirmEmailAsync(normalized, "th", DateTimeOffset.UtcNow, CancellationToken.None)).Should().BeTrue();
        (await sut.TryLinkLocalIdentityToEntraAsync(normalized, "oid-first", CancellationToken.None)).Should().BeTrue();

        (await sut.TryLinkLocalIdentityToEntraAsync(normalized, "oid-second", CancellationToken.None)).Should().BeFalse();

        (await sut.GetByNormalizedEmailAsync(normalized, CancellationToken.None))!.LinkedEntraOid.Should().Be("oid-first");
    }

    [SkippableFact]
    public async Task TryLinkLocalIdentityToEntraAsync_false_for_unknown_email()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        TestSqlConnectionFactory factory = new(fixture.ConnectionString);
        SqlTrialIdentityUserRepository sut = new(factory);

        (await sut.TryLinkLocalIdentityToEntraAsync("MISSING@EXAMPLE.COM", "oid", CancellationToken.None)).Should()
            .BeFalse();
    }
}
