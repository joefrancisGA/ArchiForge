using System.Collections.Concurrent;

using ArchLucid.Core.Billing;
using ArchLucid.Persistence.Billing;
using ArchLucid.Persistence.Connections;
using ArchLucid.TestSupport.Billing;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ArchLucid.Api.Tests.Billing;

/// <summary>GA-on marketplace webhook factory with <see cref="BillingLedgerDapperDispatchRecorder"/> over <see cref="SqlBillingLedger"/>.</summary>
internal sealed class BillingMarketplaceWebhookRecordedLedgerApiFactory : BillingMarketplaceWebhookApiFactoryBase
{
    public ConcurrentBag<string> RecordedStoredProcedureLogicalNames { get; } = [];

    protected override bool GaEnabled => true;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(
            services =>
            {
                services.RemoveAll<IBillingLedger>();
                services.AddScoped<IBillingLedger>(
                    sp =>
                    {
                        ISqlConnectionFactory connectionFactory = sp.GetRequiredService<ISqlConnectionFactory>();
                        IRlsSessionContextApplicator rls = sp.GetRequiredService<IRlsSessionContextApplicator>();
                        SqlBillingLedger inner = new(connectionFactory, rls);

                        return new BillingLedgerDapperDispatchRecorder(inner, RecordedStoredProcedureLogicalNames);
                    });
            });
    }
}
