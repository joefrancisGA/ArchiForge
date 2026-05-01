using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Persistence.Tests.Data.Repositories;

public sealed class InMemoryTenantExecDigestPreferencesRepositoryTests
{
    [SkippableFact]
    public async Task GetByTenantAsync_missing_returns_null()
    {
        InMemoryTenantExecDigestPreferencesRepository sut = new();

        ArchLucid.Contracts.Notifications.ExecDigestPreferencesResponse? row =
            await sut.GetByTenantAsync(Guid.NewGuid(), CancellationToken.None);

        row.Should().BeNull();
    }

    [SkippableFact]
    public async Task UpsertAsync_trims_recipients_and_defaults_time_zone()
    {
        InMemoryTenantExecDigestPreferencesRepository sut = new();
        Guid tenantId = Guid.NewGuid();

        ArchLucid.Contracts.Notifications.ExecDigestPreferencesResponse? row = await sut.UpsertAsync(
            tenantId,
            true,
            ["  a@x.test  ", "", "b@y.test"],
            "   ",
            dayOfWeek: 1,
            hourOfDay: 9,
            CancellationToken.None);

        row.Should().NotBeNull();
        row.RecipientEmails.Should().ContainInOrder("a@x.test", "b@y.test");
        row.IanaTimeZoneId.Should().Be("UTC");
    }

    [SkippableFact]
    public async Task ListEmailEnabledTenantIdsAsync_returns_only_enabled()
    {
        InMemoryTenantExecDigestPreferencesRepository sut = new();
        Guid onId = Guid.NewGuid();
        Guid offId = Guid.NewGuid();

        await sut.UpsertAsync(onId, true, ["a@test"], "UTC", 1, 8, CancellationToken.None);
        await sut.UpsertAsync(offId, false, ["b@test"], "UTC", 1, 8, CancellationToken.None);

        IReadOnlyList<Guid> ids = await sut.ListEmailEnabledTenantIdsAsync(CancellationToken.None);

        ids.Should().Contain(onId).And.NotContain(offId);
    }

    [SkippableFact]
    public async Task TryDisableEmailAsync_false_when_missing()
    {
        InMemoryTenantExecDigestPreferencesRepository sut = new();

        bool ok = await sut.TryDisableEmailAsync(Guid.NewGuid(), CancellationToken.None);

        ok.Should().BeFalse();
    }

    [SkippableFact]
    public async Task TryDisableEmailAsync_false_when_already_off()
    {
        InMemoryTenantExecDigestPreferencesRepository sut = new();
        Guid tenantId = Guid.NewGuid();
        await sut.UpsertAsync(tenantId, false, ["a@test"], "UTC", 1, 8, CancellationToken.None);

        bool ok = await sut.TryDisableEmailAsync(tenantId, CancellationToken.None);

        ok.Should().BeFalse();
    }

    [SkippableFact]
    public async Task TryDisableEmailAsync_true_when_was_on()
    {
        InMemoryTenantExecDigestPreferencesRepository sut = new();
        Guid tenantId = Guid.NewGuid();
        await sut.UpsertAsync(tenantId, true, ["a@test"], "UTC", 1, 8, CancellationToken.None);

        bool ok = await sut.TryDisableEmailAsync(tenantId, CancellationToken.None);

        ok.Should().BeTrue();
        ArchLucid.Contracts.Notifications.ExecDigestPreferencesResponse? row =
            await sut.GetByTenantAsync(tenantId, CancellationToken.None);
        row!.EmailEnabled.Should().BeFalse();
    }
}
