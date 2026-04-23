namespace ArchLucid.Core.Billing;

/// <summary>Resolves <see cref="IBillingProvider" /> from <c>Billing:Provider</c>.</summary>
public interface IBillingProviderRegistry
{
    IBillingProvider ResolveActiveProvider();
}
