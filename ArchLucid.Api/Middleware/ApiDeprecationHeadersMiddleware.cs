using ArchLucid.Host.Core.Configuration;

using Microsoft.Extensions.Options;

namespace ArchLucid.Api.Middleware;

/// <summary>Adds optional <c>Deprecation</c>, <c>Sunset</c>, and <c>Link</c> headers for version lifecycle communication.</summary>
public sealed class ApiDeprecationHeadersMiddleware(RequestDelegate next, IOptionsMonitor<ApiDeprecationOptions> optionsMonitor)
{
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ApiDeprecationOptions options = optionsMonitor.CurrentValue;

        if (!options.Enabled)
            return next(context);
        

        context.Response.OnStarting(() =>
        {
            if (options.EmitDeprecationTrue)
            
                context.Response.Headers.Append("Deprecation", "true");
            

            string? sunset = options.SunsetHttpDate?.Trim();
            if (!string.IsNullOrEmpty(sunset))
            
                context.Response.Headers.Append("Sunset", sunset);
            

            string? link = options.Link?.Trim();
            if (!string.IsNullOrEmpty(link))
            
                context.Response.Headers.Append("Link", link);
            

            return Task.CompletedTask;
        });

        return next(context);
    }
}
