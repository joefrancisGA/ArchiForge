using ArchLucid.Host.Core.Configuration;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace ArchLucid.Host.Core.Startup.Validation.Rules;

internal static class E2eHarnessRules
{
    public static void Collect(IConfiguration configuration, IWebHostEnvironment environment, List<string> errors)
    {
        E2eHarnessOptions o = configuration.GetSection(E2eHarnessOptions.SectionName).Get<E2eHarnessOptions>() ?? new E2eHarnessOptions();

        if (!o.Enabled)
        {
            return;
        }

        if (environment.IsProduction())
        {
            errors.Add("ArchLucid:E2eHarness:Enabled must be false in Production.");

            return;
        }

        if (string.IsNullOrWhiteSpace(o.SharedSecret) || o.SharedSecret.Trim().Length < 16)
        {
            errors.Add("ArchLucid:E2eHarness:SharedSecret must be set to a strong value (>= 16 chars) when E2eHarness is enabled.");
        }
    }
}
