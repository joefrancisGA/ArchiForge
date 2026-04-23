namespace ArchLucid.Core.Scoping;

/// <summary>
///     Resolves the current tenant / workspace / project scope (e.g. from HTTP claims or dev headers).
/// </summary>
/// <remarks>
///     Default host implementation: <c>ArchLucid.Host.Core.Auth.Services.HttpScopeContextProvider</c>, which prefers
///     <see cref="AmbientScopeContext.CurrentOverride" /> when set (e.g. advisory scan), then JWT scope claims over
///     <c>x-*-id</c> headers
///     so token-bound scope cannot be overridden by headers.
/// </remarks>
public interface IScopeContextProvider
{
    /// <summary>
    ///     Returns the active scope: ambient override if pushed, otherwise derived from the current HTTP user/headers (or
    ///     defaults in dev).
    /// </summary>
    /// <returns>Non-null <see cref="ScopeContext" />; ids may be well-known defaults when unauthenticated in development.</returns>
    ScopeContext GetCurrentScope();
}
