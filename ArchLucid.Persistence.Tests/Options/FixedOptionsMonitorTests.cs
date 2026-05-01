using ArchLucid.Persistence.Coordination.Caching;
using ArchLucid.Persistence.Options;

namespace ArchLucid.Persistence.Tests.Options;

[Trait("Category", "Unit")]
public sealed class FixedOptionsMonitorTests
{
    [Fact]
    public void Constructor_throws_when_value_null()
    {
        Action ctor = () => _ = new FixedOptionsMonitor<object>(null!);

        ctor.Should().Throw<ArgumentNullException>().WithParameterName("currentValue");
    }

    [Fact]
    public void Get_returns_CurrentValue_for_any_monitored_name()
    {
        HotPathCacheOptions options = new() { AbsoluteExpirationSeconds = 42 };
        FixedOptionsMonitor<HotPathCacheOptions> monitor = new(options);

        monitor.Get(null).AbsoluteExpirationSeconds.Should().Be(42);
        monitor.Get("does-not-exist").AbsoluteExpirationSeconds.Should().Be(42);
        monitor.CurrentValue.AbsoluteExpirationSeconds.Should().Be(42);
    }

    [Fact]
    public void OnChange_returns_no_op_disposable()
    {
        HotPathCacheOptions options = new();
        FixedOptionsMonitor<HotPathCacheOptions> monitor = new(options);

        using IDisposable subscription = monitor.OnChange((_, _) => { });

        subscription.Dispose();
        subscription.Dispose();
    }
}
