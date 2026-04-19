using System.Globalization;

using ArchLucid.Application.Tenancy;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Tenancy;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Tenancy;

[Trait("Suite", "Core")]
public sealed class TrialLifecyclePolicyTests
{
    private static readonly TrialLifecycleSchedulerOptions DefaultOpts = new();

    [Fact]
    public void TryGetNextAdvancement_active_before_expiry_returns_null()
    {
        DateTimeOffset anchor = DateTimeOffset.Parse("2026-05-01T00:00:00Z", CultureInfo.InvariantCulture);
        TenantRecord tenant = MinimalTenant(TrialLifecycleStatus.Active, anchor);

        TrialLifecycleAdvancement? adv = TrialLifecyclePolicy.TryGetNextAdvancement(
            tenant,
            anchor.AddDays(-1),
            DefaultOpts);

        adv.Should().BeNull();
    }

    [Fact]
    public void TryGetNextAdvancement_active_on_expiry_moves_to_expired()
    {
        DateTimeOffset anchor = DateTimeOffset.Parse("2026-05-01T00:00:00Z", CultureInfo.InvariantCulture);
        TenantRecord tenant = MinimalTenant(TrialLifecycleStatus.Active, anchor);

        TrialLifecycleAdvancement? adv = TrialLifecyclePolicy.TryGetNextAdvancement(
            tenant,
            anchor,
            DefaultOpts);

        adv.Should().NotBeNull();
        adv.FromStatus.Should().Be(TrialLifecycleStatus.Active);
        adv.ToStatus.Should().Be(TrialLifecycleStatus.Expired);
    }

    [Fact]
    public void TryGetNextAdvancement_expired_before_read_only_window_returns_null()
    {
        DateTimeOffset anchor = DateTimeOffset.Parse("2026-05-01T00:00:00Z", CultureInfo.InvariantCulture);
        TenantRecord tenant = MinimalTenant(TrialLifecycleStatus.Expired, anchor);

        TrialLifecycleAdvancement? adv = TrialLifecyclePolicy.TryGetNextAdvancement(
            tenant,
            anchor.AddDays(6),
            DefaultOpts);

        adv.Should().BeNull();
    }

    [Fact]
    public void TryGetNextAdvancement_expired_after_window_moves_to_read_only()
    {
        DateTimeOffset anchor = DateTimeOffset.Parse("2026-05-01T00:00:00Z", CultureInfo.InvariantCulture);
        TenantRecord tenant = MinimalTenant(TrialLifecycleStatus.Expired, anchor);

        TrialLifecycleAdvancement? adv = TrialLifecyclePolicy.TryGetNextAdvancement(
            tenant,
            anchor.AddDays(7),
            DefaultOpts);

        adv.Should().NotBeNull();
        adv.ToStatus.Should().Be(TrialLifecycleStatus.ReadOnly);
    }

    [Fact]
    public void TryGetNextAdvancement_idempotent_second_tick_same_phase_still_null_until_boundary()
    {
        DateTimeOffset anchor = DateTimeOffset.Parse("2026-05-01T00:00:00Z", CultureInfo.InvariantCulture);
        TenantRecord tenant = MinimalTenant(TrialLifecycleStatus.Expired, anchor);

        TrialLifecycleAdvancement? first = TrialLifecyclePolicy.TryGetNextAdvancement(
            tenant,
            anchor.AddDays(7),
            DefaultOpts);

        first.Should().NotBeNull();

        TenantRecord updated = MinimalTenant(TrialLifecycleStatus.ReadOnly, anchor);

        TrialLifecycleAdvancement? second = TrialLifecyclePolicy.TryGetNextAdvancement(
            updated,
            anchor.AddDays(7),
            DefaultOpts);

        second.Should().BeNull();
    }

    [Fact]
    public void ComputeDaysRemaining_for_read_only_uses_export_only_deadline()
    {
        DateTimeOffset anchor = DateTimeOffset.Parse("2026-05-01T00:00:00Z", CultureInfo.InvariantCulture);
        TenantRecord tenant = MinimalTenant(TrialLifecycleStatus.ReadOnly, anchor);
        DateTimeOffset now = anchor.AddDays(7 + 10);

        int? days = TrialLifecyclePolicy.ComputeDaysRemainingForStatusDisplay(tenant, now, DefaultOpts);

        DateTimeOffset exportOnlyStarts = anchor.AddDays(37);
        int expected = (int)Math.Floor((exportOnlyStarts - now).TotalDays);

        days.Should().Be(expected);
    }

    private static TenantRecord MinimalTenant(string status, DateTimeOffset trialExpiresUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "n",
            Slug = "s",
            Tier = TenantTier.Standard,
            CreatedUtc = DateTimeOffset.UtcNow,
            TrialExpiresUtc = trialExpiresUtc,
            TrialStatus = status,
        };
}
