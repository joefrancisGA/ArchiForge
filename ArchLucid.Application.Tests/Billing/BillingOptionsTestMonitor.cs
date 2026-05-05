using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Tests.Billing;

internal sealed class BillingOptionsTestMonitor<T>(T value) : IOptionsMonitor<T>
    where T : class
{
    public T CurrentValue
    {
        get;
    } = value;

    public T Get(string? name) => CurrentValue;

    public IDisposable? OnChange(Action<T, string?> listener) => null;
}
