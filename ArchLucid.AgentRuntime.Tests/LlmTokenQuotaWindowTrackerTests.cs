using ArchLucid.Core;
using ArchLucid.Core.Configuration;

using FluentAssertions;

using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime.Tests;

public sealed class LlmTokenQuotaWindowTrackerTests
{
    [Fact]
    public void EnsureWithinQuotaBeforeCall_when_under_limit_does_not_throw()
    {
        LlmTokenQuotaOptions opts = new()
        {
            Enabled = true,
            WindowMinutes = 60,
            MaxPromptTokensPerTenantPerWindow = 1_000_000,
            MaxCompletionTokensPerTenantPerWindow = 0
        };

        LlmTokenQuotaWindowTracker tracker = new(new StubOptionsMonitor<LlmTokenQuotaOptions>(opts));
        Guid tenant = Guid.NewGuid();

        tracker.EnsureWithinQuotaBeforeCall(tenant);
        tracker.RecordUsage(tenant, 100, 0);
        tracker.EnsureWithinQuotaBeforeCall(tenant);
    }

    [Fact]
    public void EnsureWithinQuotaBeforeCall_when_over_limit_throws()
    {
        LlmTokenQuotaOptions opts = new()
        {
            Enabled = true,
            WindowMinutes = 60,
            MaxPromptTokensPerTenantPerWindow = 150,
            AssumedMaxPromptTokensPerRequest = 50
        };

        LlmTokenQuotaWindowTracker tracker = new(new StubOptionsMonitor<LlmTokenQuotaOptions>(opts));
        Guid tenant = Guid.NewGuid();

        tracker.RecordUsage(tenant, 120, 0);

        Action act = () => tracker.EnsureWithinQuotaBeforeCall(tenant);

        act.Should().Throw<LlmTokenQuotaExceededException>();
    }

    [Fact]
    public void RecordUsage_ignores_empty_tenant()
    {
        LlmTokenQuotaOptions opts = new() { Enabled = true, WindowMinutes = 60, MaxPromptTokensPerTenantPerWindow = 1 };

        LlmTokenQuotaWindowTracker tracker = new(new StubOptionsMonitor<LlmTokenQuotaOptions>(opts));

        tracker.RecordUsage(Guid.Empty, 999, 999);
        tracker.EnsureWithinQuotaBeforeCall(Guid.Empty);
    }

    private sealed class StubOptionsMonitor<T>(T value) : IOptionsMonitor<T>
        where T : class
    {
        public T CurrentValue
        {
            get;
        } = value;

        public T Get(string? name)
        {
            return CurrentValue;
        }

        public IDisposable OnChange(Action<T, string?> listener)
        {
            return NullDisposable.Instance;
        }

        private sealed class NullDisposable : IDisposable
        {
            internal static readonly NullDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
