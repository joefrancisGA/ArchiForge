namespace ArchiForge.Host.Core.Hosting;

/// <summary>
/// Extends shutdown timeout and logs when the host begins stopping so Container Apps / K8s SIGTERM handling is observable.
/// </summary>
public static class GracefulShutdownWebApplicationBuilderExtensions
{
    /// <summary>
    /// Sets <see cref="HostOptions.ShutdownTimeout"/> to 45 seconds and registers shutdown logging.
    /// </summary>
    public static WebApplicationBuilder AddArchiForgeGracefulShutdown(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.Configure<HostOptions>(static o =>
        {
            o.ShutdownTimeout = TimeSpan.FromSeconds(45);
        });

        builder.Services.AddHostedService<GracefulShutdownNotificationHostedService>();

        return builder;
    }
}
