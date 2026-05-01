namespace ArchLucid.Application.Identity;

public interface ITrialLocalIdentityService
{
    Task<TrialLocalRegistrationResult>
        RegisterAsync(string email, string password, CancellationToken cancellationToken);

    Task<bool> VerifyEmailAsync(string email, string rawToken, CancellationToken cancellationToken);

    Task<TrialLocalAuthResult?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken);
}

public sealed class TrialLocalRegistrationResult
{
    public Guid UserId
    {
        get;
        init;
    }

    public string VerificationToken
    {
        get;
        init;
    } = string.Empty;
}

public sealed class TrialLocalAuthResult
{
    public Guid UserId
    {
        get;
        init;
    }

    public string Email
    {
        get;
        init;
    } = string.Empty;

    public string Role
    {
        get;
        init;
    } = string.Empty;
}
