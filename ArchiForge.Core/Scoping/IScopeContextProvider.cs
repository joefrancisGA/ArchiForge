namespace ArchiForge.Core.Scoping;

/// <summary>
/// Resolves the current tenant / workspace / project scope (e.g. from HTTP claims or dev headers).
/// </summary>
/// <remarks>
/// Default API implementation: <c>ArchiForge.Api.Auth.Services.HttpScopeContextProvider</c>, which prefers
/// <see cref="AmbientScopeContext.CurrentOverride"/> when set (e.g. advisory scan) so scoped services see the job’s scope without an HTTP request.
/// </remarks>
public interface IScopeContextProvider
{
    /// <summary>
    /// Returns the active scope: ambient override if pushed, otherwise derived from the current HTTP user/headers (or defaults in dev).
    /// </summary>
    /// <returns>Non-null <see cref="ScopeContext"/>; ids may be well-known defaults when unauthenticated in development.</returns>
    ScopeContext GetCurrentScope();
}
