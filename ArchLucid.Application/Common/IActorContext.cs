namespace ArchLucid.Application.Common;

/// <summary>
///     Resolves the acting principal for audit and logging (Corrected 51R). Host supplies an implementation using
///     <c>IHttpContextAccessor</c>.
/// </summary>
public interface IActorContext
{
    /// <summary>
    ///     Returns a non-empty display identity; falls back to <c>api-user</c> when no HTTP user name is available.
    /// </summary>
    string GetActor();

    /// <summary>
    ///     Returns a non-empty canonical actor key for segregation-of-duties comparisons: Entra <c>jwt:{tid}:{oid}</c>
    ///     when both <c>tid</c> and <c>oid</c> claims are present, otherwise <c>jwt:{oid}</c> when only <c>oid</c> is present,
    ///     otherwise falls back to <see cref="GetActor" /> (API key or non-JWT paths).
    /// </summary>
    string GetActorId();
}
