namespace ArchLucid.Application.Scim.Tokens;

public interface IScimBearerTokenAuthenticator
{
    Task<ScimBearerAuthenticationResult?> TryAuthenticateAsync(string plaintextToken, CancellationToken cancellationToken);
}

public sealed class ScimBearerAuthenticationResult
{
    public Guid TenantId
    {
        get;
        init;
    }

    public Guid TokenRowId
    {
        get;
        init;
    }
}
