namespace ArchiForge.Decisioning.Governance.PolicyPacks;

/// <summary>Immutable snapshot of pack content for a specific SemVer-style label.</summary>
/// <remarks>
/// <c>ContentJson</c> deserializes to <see cref="PolicyPackContentDocument"/>. Upserted by <see cref="IPolicyPackManagementService.PublishVersionAsync"/>.
/// Assignments reference <see cref="Version"/> as a string key.
/// </remarks>
public class PolicyPackVersion
{
    /// <summary>Row id.</summary>
    public Guid PolicyPackVersionId { get; set; } = Guid.NewGuid();

    /// <summary>Owning <see cref="PolicyPack"/>.</summary>
    public Guid PolicyPackId { get; set; }

    /// <summary>Version key (e.g. <c>1.0.0</c>).</summary>
    public string Version { get; set; } = null!;

    /// <summary>JSON payload for <see cref="PolicyPackContentDocument"/>.</summary>
    public string ContentJson { get; set; } = null!;

    /// <summary>When the row was created.</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>False for the initial draft row created with the pack; true after publish.</summary>
    public bool IsPublished { get; set; }
}
