using Microsoft.AspNetCore.DataProtection;

namespace ArchLucid.Application.Notifications.Email;

/// <inheritdoc cref="IExecDigestUnsubscribeTokenFactory" />
/// <remarks>Uses ASP.NET Core data protection; API and worker hosts must share key material in multi-process deployments.</remarks>
public sealed class ExecDigestUnsubscribeTokenFactory(IDataProtectionProvider dataProtectionProvider)
    : IExecDigestUnsubscribeTokenFactory
{
    private const string Purpose = "ArchLucid.ExecDigest.Unsubscribe.v1";

    private readonly IDataProtectionProvider _dataProtectionProvider =
        dataProtectionProvider ?? throw new ArgumentNullException(nameof(dataProtectionProvider));

    /// <inheritdoc />
    public string CreateToken(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));

        IDataProtector protector = _dataProtectionProvider.CreateProtector(Purpose);

        return protector.Protect(tenantId.ToString("N"));
    }

    /// <inheritdoc />
    public bool TryParseTenant(string token, out Guid tenantId)
    {
        tenantId = Guid.Empty;

        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            IDataProtector protector = _dataProtectionProvider.CreateProtector(Purpose);
            string raw = protector.Unprotect(token);

            return Guid.TryParseExact(raw, "N", out tenantId);
        }
        catch
        {
            return false;
        }
    }
}
