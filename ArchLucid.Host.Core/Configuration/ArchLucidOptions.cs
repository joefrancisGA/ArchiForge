namespace ArchiForge.Host.Core.Configuration;

public sealed class ArchiForgeOptions
{
    public const string SectionName = "ArchiForge";

    /// <summary>InMemory (tests/local) or Sql (durable).</summary>
    public string StorageProvider { get; set; } = "Sql";
}
