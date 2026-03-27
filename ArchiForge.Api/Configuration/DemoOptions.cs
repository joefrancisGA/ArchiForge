using ArchiForge.Application.Bootstrap;

namespace ArchiForge.Api.Configuration;

/// <summary>Feature flags for deterministic Contoso trusted-baseline seeding (49R pass 2 / Corrected 50R). Never enable in production-like environments.</summary>
public sealed class DemoOptions
{
    public const string SectionName = "Demo";

    /// <summary>Master switch for demo seed API and startup hook.</summary>
    public bool Enabled { get; set; }

    /// <summary>When <c>true</c> and the host is Development, runs <see cref="IDemoSeedService"/> once after DbUp.</summary>
    public bool SeedOnStartup { get; set; }
}
