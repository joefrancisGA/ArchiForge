using ArchLucid.Core.Scoping;

namespace ArchLucid.Persistence.Tests.Support;

/// <summary>Test double: callers set <see cref="Current" /> before opening SQL connections that apply RLS session context.</summary>
public sealed class MutableScopeContextProvider : IScopeContextProvider
{
    public ScopeContext Current
    {
        get;
        set;
    } = new();

    public ScopeContext GetCurrentScope()
    {
        return Current;
    }
}
