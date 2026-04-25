namespace ArchLucid.Application.Scim.Tokens;

public interface IScimTokenIssuer
{
    /// <summary>Mints a new bearer token and persists its Argon2id hash. Returns plaintext once.</summary>
    Task<ScimTokenIssueResult> IssueTokenAsync(Guid tenantId, CancellationToken cancellationToken);
}

public sealed class ScimTokenIssueResult
{
    public Guid TokenId
    {
        get;
        init;
    }

    public string PlaintextToken
    {
        get;
        init;
    } = string.Empty;

    public string PublicLookupKey
    {
        get;
        init;
    } = string.Empty;
}
