namespace ArchLucid.Application;

/// <summary>
///     How the API should map a failed submit/seed operation to HTTP (when <see cref="SubmitResultResult.Success" />
///     is false).
/// </summary>
public enum ApplicationServiceFailureKind
{
    BadRequest,
    RunNotFound,
    ResourceNotFound
}
