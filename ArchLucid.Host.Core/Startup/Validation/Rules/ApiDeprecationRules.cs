using System.Globalization;

using ArchLucid.Host.Core.Configuration;

namespace ArchLucid.Host.Core.Startup.Validation.Rules;

internal static class ApiDeprecationRules
{
    public static void Collect(IConfiguration configuration, List<string> errors)
    {
        ApiDeprecationOptions deprecation =
            configuration.GetSection(ApiDeprecationOptions.SectionName).Get<ApiDeprecationOptions>()
            ?? new ApiDeprecationOptions();

        if (!deprecation.Enabled)
            return;

        string? sunset = deprecation.SunsetHttpDate?.Trim();

        if (string.IsNullOrEmpty(sunset))
            return;

        if (!DateTimeOffset.TryParse(
                sunset,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out _))

            errors.Add(
                "ApiDeprecation:SunsetHttpDate must be empty or a parseable date when ApiDeprecation:Enabled is true.");
    }
}
