using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Scoping;

namespace ArchLucid.Persistence.Tests.Support;

/// <summary>
///     Shared <see cref="IScopeContextProvider" /> for SQL integration tests that construct repositories directly
///     (no HTTP scope). Rows get empty GUID scope triples unless the test seeds scope explicitly.
/// </summary>
internal static class PersistenceIntegrationTestScope
{
    internal static readonly IScopeContextProvider Empty = new EmptyPersistenceScopeContextProvider();
}
