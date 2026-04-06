namespace ArchiForge.Application.Common;

/// <summary>
/// Resolves the acting principal for audit and logging (Corrected 51R). Host supplies an implementation using <c>IHttpContextAccessor</c>.
/// </summary>
public interface IActorContext
{
    /// <summary>
    /// Returns a non-empty display identity; falls back to <c>api-user</c> when no HTTP user name is available.
    /// </summary>
    string GetActor();
}
