namespace ArchLucid.Host.Core.Configuration;

/// <summary>Operator/developer-facing HTTP features that must stay off in production unless explicitly enabled.</summary>
public sealed class DeveloperExperienceOptions
{
    public const string SectionName = "DeveloperExperience";

    /// <summary>When <c>true</c>, serves OpenAPI JSON, Swagger middleware JSON, and Scalar alongside <see cref="Microsoft.AspNetCore.Hosting.IHostingEnvironment"/> Development.</summary>
    public bool EnableApiExplorer
    {
        get;
        set;
    }
}
